using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;

namespace LeilaoTempoReal.API.Services;

public class LeilaoService(ILeilaoRepository repository,
                           IHubContext<LeilaoHub> hubContext,
                           IConnectionMultiplexer redisConnection)
{
    private readonly ILeilaoRepository _repository = repository;
    private readonly IHubContext<LeilaoHub> _hubContext = hubContext;
    private readonly IDatabase _redis = redisConnection.GetDatabase();

    public async Task<bool> DarLanceAsync(int leilaoId, string usuario, decimal valor)
    {
        var chave = $"leilao:{leilaoId}";

        // KEYS[1] -> Chave do leilão
        // ARGV[1] -> Valor do lance (decimal)
        // ARGV[2] -> Nome do usuário
        var script = @"
        -- LOG 1: Avisa que começou e qual chave está buscando
        redis.log(redis.LOG_WARNING, '--- DEBUG INICIO ---')
        redis.log(redis.LOG_WARNING, 'Chave buscada: ' .. KEYS[1])

        local jsonAtual = redis.call('GET', KEYS[1])
        
        if not jsonAtual then 
            redis.log(redis.LOG_WARNING, 'ERRO: Chave nao encontrada no Redis!')
            return 0 
        end

        redis.log(redis.LOG_WARNING, 'JSON Encontrado: ' .. jsonAtual)

        local leilao = cjson.decode(jsonAtual)
        
        -- LOG 2: Verifica os valores numéricos antes de comparar
        local valorLance = tonumber(ARGV[1])
        local valorAtual = tonumber(leilao.ValorAtual)

        redis.log(redis.LOG_WARNING, 'Comparando Lance: ' .. tostring(valorLance) .. ' > Atual: ' .. tostring(valorAtual))

        if valorLance > valorAtual then
            leilao.ValorAtual = valorLance
            leilao.UsuarioGanhador = ARGV[2]
            local novoJson = cjson.encode(leilao)
            redis.call('SET', KEYS[1], novoJson)
            return 1 
        else
            redis.log(redis.LOG_WARNING, 'FALHA: Lance menor que o atual')
            return 0 
        end
    ";

        var chaves = new RedisKey[] { chave };

        var valores = new RedisValue[]
        {
           valor.ToString(System.Globalization.CultureInfo.InvariantCulture),
           usuario
        };

        var resultado = await _redis.ScriptEvaluateAsync(script, chaves, valores);

        if ((int)resultado == 0)
        {
            return false;
        }

        await _hubContext.Clients.Group(leilaoId.ToString())
            .SendAsync("ReceberNovoLance", usuario, valor);

        return true;
    }
}
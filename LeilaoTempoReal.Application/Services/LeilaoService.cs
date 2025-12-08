using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace LeilaoTempoReal.Application.Services;

public class LeilaoService(INotificador notificador,
                           ILeilaoRepository repository,
                           IConnectionMultiplexer redisConnection)
{
    private readonly INotificador _notificador;
    private readonly IDatabase _redis = redisConnection.GetDatabase();
    private readonly ILeilaoRepository _repository = repository;

    public async Task<bool> DarLanceAsync(Guid leilaoId, string usuario, decimal valor)
    {
        var leilao = await _repository.ObterPorIdAsync(leilaoId);

        if (leilao == null)
        {
            return false;
        }

        var chave = $"leilao:{leilaoId}";

        var script = @"
                        local jsonAtual = redis.call('GET', KEYS[1])
                        if not jsonAtual then return 0 end 

                        local leilao = cjson.decode(jsonAtual)

                        if tonumber(ARGV[1]) > tonumber(leilao.ValorAtual) then
                            leilao.ValorAtual = tonumber(ARGV[1])
                            leilao.UsuarioGanhador = ARGV[2]
                            redis.call('SET', KEYS[1], cjson.encode(leilao))
                            return 1
                        else
                            return 0
                        end
                    ";

        var valores = new RedisValue[]
        {
        valor.ToString(System.Globalization.CultureInfo.InvariantCulture),
        usuario
        };

        var resultado = await _redis.ScriptEvaluateAsync(script, new RedisKey[] { chave }, valores);

        if ((int)resultado == 0) return false;

        await _notificador.NotificarNovoLance(leilaoId, usuario, valor);

        return true;
    }
}
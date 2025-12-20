using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Dominio.Interfaces;
using MassTransit;
using StackExchange.Redis;
using System.Drawing;
using System.Text.Json;

namespace LeilaoTempoReal.Application.Services;

public class LeilaoService(INotificador notificador,
                           ILeilaoRepository repository,
                           IConnectionMultiplexer redisConnection,
                           IPublishEndpoint bus) : ILeilaoService
{
    private readonly INotificador _notificador = notificador;
    private readonly IDatabase _redis = redisConnection.GetDatabase();
    private readonly ILeilaoRepository _repository = repository;
    private readonly IPublishEndpoint _bus = bus;

    public async Task<Result> DarLanceAsync(Guid leilaoId, decimal valor, string usuario)
    {
        if (string.IsNullOrEmpty(usuario)) return Result.Fail("Usuário inválido.");

        var leilao = await _repository.ObterPorIdAsync(leilaoId);

        if (leilao == null) return Result.Fail("Leilão não encontrado.");
        if (DateTime.Now >= leilao.DataFim) return Result.Fail("O leilão já encerrou.");

        var chave = $"leilao:{leilaoId}";

        var script = @"
                    local jsonAtual = redis.call('GET', KEYS[1])
                    if not jsonAtual then return {0, 0} end -- Retorna array: {Erro, 0}

                    local leilao = cjson.decode(jsonAtual)
                    local valorAtual = tonumber(leilao.ValorAtual)
                    local novoLance = tonumber(ARGV[1])

                    if novoLance > valorAtual then
                        leilao.ValorAtual = novoLance
                        leilao.UsuarioGanhador = ARGV[2]
                        redis.call('SET', KEYS[1], cjson.encode(leilao))
                        return {1, novoLance} -- {Sucesso, NovoValor}
                    else
                        return {2, valorAtual} -- {Rejeitado, ValorQueGanhou}
                    end
                ";

        var valores = new RedisValue[]
                {
            valor.ToString(System.Globalization.CultureInfo.InvariantCulture),
            usuario
                };

        var retornoRedis = await _redis.ScriptEvaluateAsync(script, [chave], valores);

        if (retornoRedis.IsNull)
        {
            return Result.Fail("Erro crítico: O script Redis não retornou resultado.");
        }

        var resultadoArray = (RedisResult[])retornoRedis!;

        var codigoRetorno = (int)resultadoArray[0];

        var valorNoRedis = (decimal)(double)resultadoArray[1];

        switch (codigoRetorno)
        {
            case 0:
                return Result.Fail("Erro interno: Leilão não encontrado no cache.");

            case 2:
                return Result.Fail($"Lance rejeitado. O valor deve ser superior a {valorNoRedis.ToString("C")}.");

            case 1:
                break; 

            default:
                return Result.Fail("Erro desconhecido ao processar lance.");
        }

        await _bus.Publish(new LanceCriadoEvent(leilaoId, valor, usuario));
        await _notificador.NotificarNovoLance(leilaoId, usuario, valor);

        return Result.Success();
    }

    public async Task<Result> FinalizarLeilaoAsync(Guid leilaoId)
    {
        var leilao = await _repository.ObterPorIdAsync(leilaoId);

        if (leilao == null) return Result.Fail("Leilão não encontrado.");
        if (leilao.Finalizado) return Result.Fail("Leilão já foi finalizado.");

        await _bus.Publish(new LeilaoFinalizadoEvent(leilaoId));

        leilao.Finalizar(); 

        var jsonAtualizado = JsonSerializer.Serialize(leilao);
        var chave = $"leilao:{leilaoId}";

        await _redis.StringSetAsync(chave, jsonAtualizado);

        await _redis.KeyExpireAsync(chave, TimeSpan.FromMinutes(10));

        await _notificador.NotificarLeilaoFinalizado(leilaoId, leilao.UsuarioGanhador ?? "Ninguém", leilao.ValorAtual);

        return Result.Success();
    }
}
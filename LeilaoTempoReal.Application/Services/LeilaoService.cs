using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Dominio.Interfaces;
using StackExchange.Redis;
using MassTransit;

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

        var resultadoRedis = (RedisResult[])await _redis.ScriptEvaluateAsync(script, [chave], valores);

        var codigoRetorno = (int)resultadoRedis[0];

        var valorNoRedis = (decimal)(double)resultadoRedis[1];

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
}
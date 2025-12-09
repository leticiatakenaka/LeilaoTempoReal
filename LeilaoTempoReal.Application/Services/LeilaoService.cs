using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Dominio.Interfaces;
using StackExchange.Redis;

namespace LeilaoTempoReal.Application.Services;

public class LeilaoService(INotificador notificador,
                           ILeilaoRepository repository,
                           IConnectionMultiplexer redisConnection,
                           ILanceChannel lanceChannel) : ILeilaoService
{
    private readonly INotificador _notificador = notificador;
    private readonly IDatabase _redis = redisConnection.GetDatabase();
    private readonly ILeilaoRepository _repository = repository;
    private readonly ILanceChannel _lanceChannel = lanceChannel;

    public async Task<Result> DarLanceAsync(Guid leilaoId, decimal valor, string usuario)
    {
        if (string.IsNullOrEmpty(usuario))
            return Result.Fail("Usuário inválido.");

        var leilao = await _repository.ObterPorIdAsync(leilaoId);

        if (leilao == null)
        {
            return Result.Fail("Leilão não encontrado.");
        }

        if (DateTime.Now >= leilao.DataFim)
        {
            return Result.Fail("O leilão já encerrou.");
        }

        var chave = $"leilao:{leilaoId}";

        var script = @"
                        local jsonAtual = redis.call('GET', KEYS[1])
                        if not jsonAtual then return 0 end -- Código 0: Erro técnico

                        local leilao = cjson.decode(jsonAtual)

                        if tonumber(ARGV[1]) > tonumber(leilao.ValorAtual) then
                            leilao.ValorAtual = tonumber(ARGV[1])
                            leilao.UsuarioGanhador = ARGV[2]
                            redis.call('SET', KEYS[1], cjson.encode(leilao))
                            return 1 -- Código 1: Sucesso
                        else
                            return 2 -- Código 2: Lance baixo
                        end
                    ";

        var valores = new RedisValue[]
        {
            valor.ToString(System.Globalization.CultureInfo.InvariantCulture),
            usuario
        };

        var resultadoRedis = await _redis.ScriptEvaluateAsync(script, [chave], valores);
        var codigoRetorno = (int)resultadoRedis;

        switch (codigoRetorno)
        {
            case 0:
                return Result.Fail("Erro interno: Leilão não encontrado no cache.");

            case 2:
                return Result.Fail($"Lance rejeitado. O valor deve ser superior a {leilao.ValorAtual.ToString("C")}.");

            case 1:
                break;

            default:
                return Result.Fail("Erro desconhecido ao processar lance.");
        }

        await _notificador.NotificarNovoLance(leilaoId, usuario, valor);

        _lanceChannel.TentarEscreverLance(leilaoId, valor, usuario);

        return Result.Success();
    }
}
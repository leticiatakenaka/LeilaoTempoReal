using LeilaoTempoReal.Application.Common;

namespace LeilaoTempoReal.Application.Services;

public interface ILeilaoService
{
    Task<Result> DarLanceAsync(Guid leilaoId, decimal valor, string usuario);
}
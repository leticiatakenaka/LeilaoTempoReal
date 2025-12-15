namespace LeilaoTempoReal.Application.Common;

public record LanceCriadoEvent(Guid LeilaoId, decimal Valor, string Usuario);
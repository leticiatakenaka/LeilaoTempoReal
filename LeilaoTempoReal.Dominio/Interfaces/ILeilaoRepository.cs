using LeilaoTempoReal.Dominio.Entidades;

namespace LeilaoTempoReal.Dominio.Interfaces;

public interface ILeilaoRepository
{
    Task<Leilao?> ObterPorIdAsync(Guid id);
}
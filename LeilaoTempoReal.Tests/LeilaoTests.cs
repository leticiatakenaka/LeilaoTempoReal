using LeilaoTempoReal.Dominio.Entidades;
using Xunit;

namespace LeilaoTempoReal.Tests;

public class LeilaoTests
{
    [Fact]
    public void Finalizar_DeveAtualizarPropriedadeFinalizado_QuandoChamado()
    {
        var leilao = new Leilao("Item Teste", 100m, DateTime.Now.AddDays(1));

        leilao.Finalizar();

        Assert.True(leilao.Finalizado);
    }

    [Fact]
    public void Finalizar_NaoDeveDarErro_SeChamadoDuasVezes()
    {
        var leilao = new Leilao("Item Teste", 100m, DateTime.Now.AddDays(1));

        leilao.Finalizar();
        leilao.Finalizar(); 

        Assert.True(leilao.Finalizado);
    }
}
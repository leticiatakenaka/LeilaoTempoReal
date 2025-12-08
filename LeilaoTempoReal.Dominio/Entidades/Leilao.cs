namespace LeilaoTempoReal.Dominio.Entidades;

public class Leilao
{
    public int Id { get; private set; }
    public string Nome { get; private set; }
    public decimal ValorInicial { get; private set; }
    public decimal ValorAtual { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }
    public bool Finalizado { get; private set; }

    public string? UsuarioGanhador { get; set; }

    protected Leilao()
    {
        Nome = string.Empty;
    }

    public Leilao(string nome, decimal valorInicial, DateTime dataFim)
    {
        if (valorInicial < 0) throw new ArgumentException("Valor não pode ser negativo");
        if (dataFim < DateTime.Now) throw new ArgumentException("Data fim deve ser futura");

        Nome = nome;
        ValorInicial = valorInicial;
        ValorAtual = valorInicial;
        DataInicio = DateTime.Now;
        DataFim = dataFim;
        Finalizado = false;
        UsuarioGanhador = null;
    }

    public void Finalizar()
    {
        Finalizado = true;
    }
}
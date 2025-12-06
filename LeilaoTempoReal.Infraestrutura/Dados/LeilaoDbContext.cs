using LeilaoTempoReal.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace LeilaoTempoReal.Infraestrutura.Dados;

public class LeilaoDbContext : DbContext
{
    public LeilaoDbContext(DbContextOptions<LeilaoDbContext> options) : base(options)
    {
    }

    public DbSet<Leilao> Leiloes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Leilao>()
            .Property(l => l.ValorAtual)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Leilao>()
           .Property(l => l.ValorInicial)
           .HasPrecision(18, 2);
    }
}
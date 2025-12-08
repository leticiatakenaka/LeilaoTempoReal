using System.Text.Json;
using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using LeilaoTempoReal.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LeilaoTempoReal.Infraestrutura.Repositorios;

public class LeilaoRepository(LeilaoDbContext context, IConnectionMultiplexer redisConnection) : ILeilaoRepository
{
    private readonly LeilaoDbContext _context = context;
    private readonly IDatabase _redis = redisConnection.GetDatabase();

    public async Task<Leilao?> ObterPorIdAsync(Guid id)
    {
        var chave = $"leilao:{id}";

        var jsonRedis = await _redis.StringGetAsync(chave);

        if (!jsonRedis.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<Leilao>(jsonRedis.ToString());
        }

        var leilao = await _context.Leiloes.FirstOrDefaultAsync(l => l.Id == id);

        if (leilao != null)
        {
            var jsonParaSalvar = JsonSerializer.Serialize(leilao);
            await _redis.StringSetAsync(chave, jsonParaSalvar, TimeSpan.FromHours(1));
        }

        return leilao;
    }
}
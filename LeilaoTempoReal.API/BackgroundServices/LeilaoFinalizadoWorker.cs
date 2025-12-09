using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.Infraestrutura.Dados;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LeilaoTempoReal.API.BackgroundServices;

public class LeilaoFinalizadoWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeilaoFinalizadoWorker> _logger;
    private readonly IHubContext<LeilaoHub> _hubContext;

    private readonly TimeSpan _intervalo = TimeSpan.FromSeconds(5);

    public LeilaoFinalizadoWorker(IServiceProvider serviceProvider,
                                  ILogger<LeilaoFinalizadoWorker> logger,
                                  IHubContext<LeilaoHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--- Worker de Finalização de Leilão Iniciado ---");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<LeilaoDbContext>();

                var leiloesVencidos = await dbContext.Leiloes
                    .Where(l => l.DataFim <= DateTime.Now && !l.Finalizado)
                    .ToListAsync(stoppingToken);

                foreach (var leilao in leiloesVencidos)
                {
                    leilao.Finalizar();
                    _logger.LogInformation($"Finalizando leilão: {leilao.Nome}. Ganhador: {leilao.UsuarioGanhador}");

                    await _hubContext.Clients.Group(leilao.Id.ToString())
                        .SendAsync("LeilaoFinalizado", leilao.UsuarioGanhador, leilao.ValorAtual, stoppingToken);
                }

                if (leiloesVencidos.Count > 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar finalização de leilões");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }
}
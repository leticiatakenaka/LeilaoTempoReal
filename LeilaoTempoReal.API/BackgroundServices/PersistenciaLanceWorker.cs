using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Infraestrutura.Dados;

namespace LeilaoTempoReal.API.BackgroundServices;

public class PersistenciaLanceWorker : BackgroundService
{
    private readonly ILanceChannel _channel;
    private readonly IServiceProvider _serviceProvider; // Necessário para criar escopo do banco
    private readonly ILogger<PersistenciaLanceWorker> _logger;

    public PersistenciaLanceWorker(ILanceChannel channel, IServiceProvider serviceProvider, ILogger<PersistenciaLanceWorker> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--- Worker de Persistência Iniciado ---");

        await foreach (var item in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<LeilaoDbContext>();

                var leilao = await dbContext.Leiloes.FindAsync([item.LeilaoId], cancellationToken: stoppingToken);

                if (leilao != null)
                {
                    leilao.ReceberLance(item.Valor, item.Usuario);

                    await dbContext.SaveChangesAsync(stoppingToken);

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            "Lance persistido no SQL: {Valor} por {Usuario}",
                            item.Valor,
                            item.Usuario
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao persistir lance no SQL");
            }
        }
    }
}
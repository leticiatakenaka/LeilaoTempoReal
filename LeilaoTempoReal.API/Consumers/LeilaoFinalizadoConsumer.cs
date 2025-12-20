using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Infraestrutura.Dados;
using MassTransit;

namespace LeilaoTempoReal.API.Consumers;

public class LeilaoFinalizadoConsumer(LeilaoDbContext dbContext, ILogger<LeilaoFinalizadoConsumer> logger) : IConsumer<LeilaoFinalizadoEvent>
{
    public async Task Consume(ConsumeContext<LeilaoFinalizadoEvent> context)
    {
        var dados = context.Message;

        var leilao = await dbContext.Leiloes.FindAsync(dados.LeilaoId);

        if (leilao != null)
        {
            leilao.Finalizar();
            await dbContext.SaveChangesAsync();

            logger.LogInformation($"[RabbitMQ] Leilão finalizado: {dados.LeilaoId}");
        }
    }
}

using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Infraestrutura.Dados;
using MassTransit;

namespace LeilaoTempoReal.API.Consumers;

public class LanceCriadoConsumer(LeilaoDbContext dbContext, ILogger<LanceCriadoConsumer> logger) : IConsumer<LanceCriadoEvent>
{
    public async Task Consume(ConsumeContext<LanceCriadoEvent> context)
    {
        var dados = context.Message; 

        var leilao = await dbContext.Leiloes.FindAsync(dados.LeilaoId);

        if (leilao != null)
        {
            leilao.ReceberLance(dados.Valor, dados.Usuario);
            await dbContext.SaveChangesAsync();

            logger.LogInformation($"[RabbitMQ] Lance persistido: {dados.Valor} de {dados.Usuario}");
        }
    }
}
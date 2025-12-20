using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.Dominio.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LeilaoTempoReal.API.Services;

public class NotificadorSignalR(IHubContext<LeilaoHub> hubContext) : INotificador
{
    private readonly IHubContext<LeilaoHub> _hubContext = hubContext;

    public async Task NotificarNovoLance(Guid leilaoId, string usuario, decimal valor)
    {
        await _hubContext.Clients.Group(leilaoId.ToString())
            .SendAsync("ReceberNovoLance", usuario, valor);
    }

    public async Task NotificarLeilaoFinalizado(Guid leilaoId, string usuario, decimal valor)
    {
        await _hubContext.Clients.Group(leilaoId.ToString())
            .SendAsync("ReceberFinalizado", usuario, valor);
    }
}
using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.Dominio.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LeilaoTempoReal.API.Services;

public class NotificadorSignalR : INotificador
{
    private readonly IHubContext<LeilaoHub> _hubContext;

    public NotificadorSignalR(IHubContext<LeilaoHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotificarNovoLance(Guid leilaoId, string usuario, decimal valor)
    {
        await _hubContext.Clients.Group(leilaoId.ToString())
            .SendAsync("ReceberNovoLance", usuario, valor);
    }
}
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace LeilaoTempoReal.API.Hubs
{
    public class LeilaoHub : Hub
    {
        public async Task EntrarNoLeilao(int leilaoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, leilaoId.ToString());

            await Clients.Caller.SendAsync("Notificacao", $"Você entrou no leilão {leilaoId}");
        }

        public async Task SairDoLeilao(int leilaoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, leilaoId.ToString());
        }
    }
}

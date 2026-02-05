using Microsoft.AspNetCore.SignalR;

namespace ChatUp.SlaHub;
public class SlaHub : Hub
{
    // Send update to all clients
    public async Task BroadcastSlaUpdate(int ticketId, string countdown)
    {
        await Clients.All.SendAsync("ReceiveSlaUpdate", ticketId, countdown);
    }
}

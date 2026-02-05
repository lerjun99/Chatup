using Microsoft.AspNetCore.SignalR;

namespace ChatUp.UserStatusHub
{
    public class UserStatusHub : Hub
    {
        // Clients don’t call this directly, backend will push events
        public async Task SendStatus(int userId, bool isOnline)
        {
            await Clients.All.SendAsync("ReceiveStatus", userId, isOnline);
        }
    }
}

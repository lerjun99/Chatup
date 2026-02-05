using Microsoft.AspNetCore.SignalR;
using Models;

namespace ChatUp.Hubs
{
    public class ChatHub : Hub
    {
        // Map UserId to connection ID
        private static readonly Dictionary<int, string> Users = new();

        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext.Request.Query.TryGetValue("userId", out var userIdStr) && int.TryParse(userIdStr, out var userId))
            {
                Users[userId] = Context.ConnectionId;
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userEntry = Users.FirstOrDefault(u => u.Value == Context.ConnectionId);
            if (userEntry.Key != 0)
            {
                Users.Remove(userEntry.Key);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(ChatMessage msg)
        {
            // Send to receiver if connected
            if (Users.TryGetValue(msg.ReceiverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection).SendAsync("ReceiveMessage", msg);
            }

            // Also send back to sender so sender sees it instantly
            if (Users.TryGetValue(msg.SenderId, out var senderConnection))
            {
                await Clients.Client(senderConnection).SendAsync("ReceiveMessage", msg);
            }
        }
    }
}

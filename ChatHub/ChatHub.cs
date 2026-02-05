using Microsoft.AspNetCore.SignalR;
using static MOD.MOD;

namespace ChatUp.Hubs
{
    public class ChatHub : Hub
    {
        // Optional: track connected users
        private static readonly Dictionary<string, int> _connections = new();

        public override Task OnConnectedAsync()
        {
            // Access HttpContext safely
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.Query.ContainsKey("userId"))
            {
                var userIdStr = httpContext.Request.Query["userId"].ToString();
                if (int.TryParse(userIdStr, out int userId))
                {
                    _connections[Context.ConnectionId] = userId;
                }
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _connections.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // Broadcast message to sender and receiver
        public async Task SendMessage(ChatMessage message)
        {
            // Send to receiver
            if (!string.IsNullOrEmpty(message.ReceiverId.ToString()))
            {
                await Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", message);
            }

            // Send back to sender
            if (!string.IsNullOrEmpty(message.SenderId.ToString()))
            {
                await Clients.User(message.SenderId.ToString()).SendAsync("ReceiveMessage", message);
            }
        }
    }


}

using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class ChatHub : Hub
    {
        // Map UserId to connection ID
        private static readonly ConcurrentDictionary<int, string> Users = new();

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.Request.Query.TryGetValue("userId", out var userId) == true)
            {
                int uid = int.Parse(userId!);
                Users[uid] = Context.ConnectionId;

                // 🟢 Notify everyone this user is online
                await Clients.All.SendAsync("UserStatusChanged", uid, true, DateTime.UtcNow);
            }

            await base.OnConnectedAsync();
        }
        public async Task BroadcastTicketUpdate()
        {
            await Clients.All.SendAsync("ReceiveTicketUpdate");
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userEntry = Users.FirstOrDefault(u => u.Value == Context.ConnectionId);
            if (userEntry.Key != 0)
            {
                Users.TryRemove(userEntry.Key, out _);

                // 🔴 Notify everyone this user went offline
                await Clients.All.SendAsync("UserStatusChanged", userEntry.Key, false, DateTime.UtcNow);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendTypingStatus(int senderId, int receiverId, bool isTyping)
        {
            // Notify receiver if connected
            if (Users.TryGetValue(receiverId, out var receiverConnection))
            {
                await Clients.Client(receiverConnection)
                    .SendAsync("ReceiveTypingStatus", senderId, receiverId, isTyping);
            }
        }
        public async Task UserOnline(int userId)
        {
            Users[userId] = Context.ConnectionId;
            await Clients.All.SendAsync("UserStatusChanged", userId, true, DateTime.UtcNow);
        }

        public async Task UserOffline(int userId)
        {
            if (Users.TryRemove(userId, out _))
            {
                await Clients.All.SendAsync("UserStatusChanged", userId, false, DateTime.UtcNow);
            }
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
        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public static string GetConversationGroupName(int conversationId) => $"conversation-{conversationId}";

        // optional client-invokable method to send message via hub (you can either send via API and hub context, or directly via hub)
        public async Task SendMessageToConversation(int conversationId, object messageDto)
        {
            await Clients.Group(GetConversationGroupName(conversationId)).SendAsync("ReceiveMessage", messageDto);
        }
        public async Task SendTicketMessageToConversation(int conversationId, MessageDto message)
        {
            await Clients.Group(GetConversationGroupName(conversationId))
                         .SendAsync("ReceiveTicketMessage", message);
        }
        public async Task JoinTicketGroup(int ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Ticket-{ticketId}");
        }

        public async Task LeaveTicketGroup(int ticketId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Ticket-{ticketId}");
        }
    }
}

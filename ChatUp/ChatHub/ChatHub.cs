using ChatUp.Application.Auth.Commands.ChatUp.Application.Features.LoginHistory.Commands;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.LoginHistory.Commands;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ChatUp.ChatHub
{
    public class ChatHub : Hub
    {
        private readonly IChatHubContext _chatHubContext;
        private readonly IMediator _mediator;

        // Thread-safe dictionaries
        private static readonly ConcurrentDictionary<int, string> Users = new();
        private static readonly ConcurrentDictionary<int, DateTime> LastLoginTime = new();
        private static readonly ConcurrentDictionary<int, bool> UserStatuses = new();

        public ChatHub(IMediator mediator, IChatHubContext chatHubContext)
        {
            _mediator = mediator;
            _chatHubContext = chatHubContext;
        }
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext.Request.Query.TryGetValue("userId", out var userIdStr)
                && int.TryParse(userIdStr, out var userId))
            {
                Users[userId] = Context.ConnectionId;
                LastLoginTime[userId] = DateTime.UtcNow;
                UserStatuses[userId] = true;

                await _mediator.Send(new RecordLoginCommand(userId));

                await Clients.All.SendAsync("UserStatusChanged", userId, true, LastLoginTime[userId]);
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
                UserStatuses[userEntry.Key] = false;

                await _mediator.Send(new RecordLogoutCommand(userEntry.Key));

                var lastLogin = LastLoginTime.TryGetValue(userEntry.Key, out var dt) ? dt : DateTime.UtcNow;
                await Clients.All.SendAsync("UserStatusChanged", userEntry.Key, false, lastLogin);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Messaging

        public async Task SendMessage(ChatMessage msg)
        {
            // Send to receiver
            if (Users.TryGetValue(msg.ReceiverId, out var receiverConnection))
                await Clients.Client(receiverConnection).SendAsync("ReceiveMessage", msg);

            // Send back to sender
            if (Users.TryGetValue(msg.SenderId, out var senderConnection))
                await Clients.Client(senderConnection).SendAsync("ReceiveMessage", msg);
        }

        public async Task SendTicketMessage(MessageDto message)
        {
            await Clients.Group(GetConversationGroupName(message.TicketId))
                .SendAsync("ReceiveTicketMessage", message);
        }
        public async Task SendTicketMessageToConversation(int conversationId, MessageDto message)
        {
            await Clients.Group(GetConversationGroupName(conversationId))
                         .SendAsync("ReceiveTicketMessage", message);
        }
        public async Task SendMessageToConversation(int conversationId, ChatMessage message)
        {
            await _chatHubContext.SendMessageToConversation(conversationId, message);
        }

        public async Task SendTypingStatus(int senderId, int receiverId, bool isTyping)
        {
            if (Users.TryGetValue(receiverId, out var receiverConnection))
                await Clients.Client(receiverConnection).SendAsync("ReceiveTypingStatus", senderId, receiverId, isTyping);
        }

        #endregion

        #region Conversation Groups

        private static string GetConversationGroupName(int conversationId) => $"conversation-{conversationId}";

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        }

        public async Task LockConversation(int conversationId)
        {
            await _chatHubContext.NotifyConversationLocked(conversationId);
        }

        #endregion

        #region User Status

        public async Task UserOnline(int userId)
        {
            Users[userId] = Context.ConnectionId;
            UserStatuses[userId] = true;
            LastLoginTime[userId] = DateTime.UtcNow;

            await Clients.All.SendAsync("UserStatusChanged", userId, true, LastLoginTime[userId]);
        }

        public async Task UserOffline(int userId)
        {
            Users.TryRemove(userId, out _);
            UserStatuses[userId] = false;

            await Clients.All.SendAsync("UserStatusChanged", userId, false, DateTime.UtcNow);
        }

        public Task<List<object>> GetAllUserStatuses()
        {
            var result = UserStatuses.Select(u => new
            {
                UserId = u.Key,
                IsOnline = u.Value,
                LastLogin = LastLoginTime.ContainsKey(u.Key) ? LastLoginTime[u.Key] : (DateTime?)null
            }).ToList<object>();

            return Task.FromResult(result);
        }

        #endregion
    }
}

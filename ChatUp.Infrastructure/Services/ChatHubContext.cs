using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class ChatHubContext : IChatHubContext
    {
        private readonly IHubContext<ChatHub> _hub;

        public ChatHubContext(IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }
        public string GetConversationGroupName(int conversationId) => $"conversation-{conversationId}";
        public Task NotifyConversationLocked(int conversationId)
        {
            return _hub.Clients.Group(ChatHub.GetConversationGroupName(conversationId))
                .SendAsync("ConversationLocked", new { ConversationId = conversationId });
        }

        public Task SendMessageToConversation(int conversationId, ChatMessage message)
        {
            return _hub.Clients.Group(ChatHub.GetConversationGroupName(conversationId))
    .SendAsync("ReceiveTicketMessage", message);
        }
        public Task SendTicketMessageToConversation(int conversationId, MessageDto message)
        {
            return _hub.Clients.Group(GetConversationGroupName(conversationId))
                .SendAsync("ReceiveTicketMessage", message);
        }
        public Task SendTicketMessageToUsers(IEnumerable<int> userIds, MessageDto message)
        {
            var groups = userIds.Distinct().Select(ChatHub.GetUserGroupName).ToList();
            return groups.Count == 0
                ? Task.CompletedTask
                : _hub.Clients.Groups(groups).SendAsync("ReceiveTicketMessage", message);
        }
        public Task SendNotificationToUser(int userId, object notification) =>
            _hub.Clients.Group(ChatHub.GetUserGroupName(userId)).SendAsync("ReceiveNotification", notification);

        public Task NotifyDashboardChanged() => _hub.Clients.All.SendAsync("ReceiveDashboardUpdate");

        public async Task NotifyTicketUpdated()
        {
            await _hub.Clients.All.SendAsync("ReceiveTicketUpdate");
        }
    }
}

using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IChatHubContext
    {
        string GetConversationGroupName(int conversationId);
        Task SendMessageToConversation(int conversationId, ChatMessage message);
        Task SendTicketMessageToConversation(int ticketId, MessageDto message);
        /// <summary>Delivers a ticket message to these users on whichever page they have open (their personal group).</summary>
        Task SendTicketMessageToUsers(IEnumerable<int> userIds, MessageDto message);
        /// <summary>Pushes one bell notification to its recipient ("ReceiveNotification").</summary>
        Task SendNotificationToUser(int userId, object notification);
        /// <summary>Lighter than NotifyTicketUpdated: only dashboards react ("ReceiveDashboardUpdate"), e.g. after a chat reply.</summary>
        Task NotifyDashboardChanged();
        Task NotifyConversationLocked(int conversationId);
        Task NotifyTicketUpdated();
    }
}

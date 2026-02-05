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
        Task NotifyConversationLocked(int conversationId);
        Task NotifyTicketUpdated();
    }
}

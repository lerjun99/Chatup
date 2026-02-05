using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Messages.Commands;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.Handlers
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, int>
    {
        private readonly IChatDBContext _context;
        private readonly IChatHubContext _hub;

        public SendMessageCommandHandler(IChatDBContext context, IChatHubContext hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task<int> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            var message = new ChatMessage
            {
                ConversationId = request.ConversationId,
                SenderId = request.SenderId,
                ReceiverId = request.ReceiverId,
                Text = request.Text,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);

            // Broadcast to conversation group
            await _hub.SendMessageToConversation(request.ConversationId, message);

            return message.Id;
        }
    }

}

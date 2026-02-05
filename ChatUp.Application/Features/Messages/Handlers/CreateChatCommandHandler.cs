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
    public class CreateChatCommandHandler : IRequestHandler<CreateChatCommand, int>
    {
        private readonly IChatDBContext _context;
        private readonly IChatHubContext _chatHub;

        public CreateChatCommandHandler(IChatDBContext context, IChatHubContext chatHub)
        {
            _context = context;
            _chatHub = chatHub;
        }

        public async Task<int> Handle(CreateChatCommand request, CancellationToken cancellationToken)
        {
            var conversation = new ChatConversation
            {
                Name = request.Name,
                IsGroup = request.IsGroup,
                CreatedAt = DateTime.UtcNow,
                IsLocked = false
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync(cancellationToken);

            // Add participants
            foreach (var userId in request.UserIds)
            {
                _context.ChatParticipants.Add(new ChatParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = userId,
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Optional: Notify participants via SignalR
            foreach (var userId in request.UserIds)
            {
                await _chatHub.SendMessageToConversation(conversation.Id, new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Text = $"Conversation '{conversation.Name}' created",
                    Timestamp = DateTime.UtcNow
                });
            }
            return conversation.Id;
        }
    }
}

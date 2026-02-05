using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Messages.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.Handlers
{
    public class CloseConversationCommandHandler : IRequestHandler<CloseConversationCommand, Unit>
    {
        private readonly IChatDBContext _context;
        private readonly IChatHubContext _chatHub; // Not IHubContext<ChatHub>

        public CloseConversationCommandHandler(IChatDBContext context, IChatHubContext chatHub)
        {
            _context = context;
            _chatHub = chatHub;
        }

        public async Task<Unit> Handle(CloseConversationCommand request, CancellationToken cancellationToken)
        {
            var conv = await _context.ChatConversations
                .FindAsync(new object[] { request.ConversationId }, cancellationToken);

            if (conv == null)
                throw new KeyNotFoundException("Conversation not found");

            conv.IsLocked = true;
            await _context.SaveChangesAsync(cancellationToken);

            await _chatHub.NotifyConversationLocked(request.ConversationId);

            return Unit.Value;
        }
    }
}

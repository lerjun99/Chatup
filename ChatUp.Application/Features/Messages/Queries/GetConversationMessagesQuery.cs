using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.Queries
{
    public class GetConversationMessagesQuery : IRequest<List<ChatMessage>>
    {
        public int ConversationId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; } = 20;
    }

    public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, List<ChatMessage>>
    {
        private readonly IChatDBContext _context;

        public GetConversationMessagesQueryHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessage>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Set<ChatMessage>()
                .Where(m => m.ConversationId == request.ConversationId)
                .OrderByDescending(m => m.Timestamp)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);
        }
    }

}

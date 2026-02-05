using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Messages.DTOs;
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
    public class GetUserConversationsQuery : IRequest<List<ChatConversationDto>>
    {
        public int UserId { get; set; }
    }

    public class GetUserConversationsQueryHandler : IRequestHandler<GetUserConversationsQuery, List<ChatConversationDto>>
    {
        private readonly IChatDBContext _context;

        public GetUserConversationsQueryHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<ChatConversationDto>> Handle(GetUserConversationsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Set<ChatConversation>()
                .Where(c => c.Participants.Any(p => p.UserId == request.UserId))
                .Select(c => new ChatConversationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsGroup = c.IsGroup,
                    UserIds = c.Participants.Select(p => p.UserId).ToList()
                })
                .ToListAsync(cancellationToken);
        }
    }

}

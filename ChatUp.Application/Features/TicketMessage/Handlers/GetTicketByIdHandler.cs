using AutoMapper;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class GetTicketByIdHandler : IRequestHandler<GetTicketByIdQueries, TicketDto?>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;

        public GetTicketByIdHandler(IChatDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TicketDto?> Handle(GetTicketByIdQueries request, CancellationToken cancellationToken)
        {
            var ticket = await _context.Tickets
         .Include(t => t.Messages)
         .Include(t => t.Project) // ✅ Include the Project entity
         .AsNoTracking()
         .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null)
                return null;

            var dto = _mapper.Map<TicketDto>(ticket);
            dto.ProjectTitle = ticket.Project?.Title ?? string.Empty;
            // Add sender avatar for each message
            foreach (var message in dto.Messages ?? Enumerable.Empty<MessageDto>())
            {
                // Look up the sender from your Users table (assuming _context.Users exists)
                var user = await _context.UserAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == message.SenderId, cancellationToken);

                message.SenderAvatar = user.Uploads != null && user.Uploads.Any()
                        ? user.Uploads.First().Base64Content
                        : "images/default.png";
            }

            return dto;
        }
    }
}

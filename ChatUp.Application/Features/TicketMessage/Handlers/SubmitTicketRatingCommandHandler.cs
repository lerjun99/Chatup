using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class SubmitTicketRatingCommandHandler : IRequestHandler<SubmitTicketRatingCommand, bool>
    {
        private readonly IChatDBContext _context;

        public SubmitTicketRatingCommandHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(SubmitTicketRatingCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _context.Tickets
          .Include(t => t.SupportedBy)
          .Include(t => t.Client)
          .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null || ticket.Client == null || ticket.SupportedBy == null)
                return false;

            // Lookup the user account submitting the rating
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == request.ClientId, cancellationToken);

            if (user == null || user.ClientId != ticket.ClientId)
                return false; // user is not the client related to this ticket

            var rating = new TicketRating
            {
                TicketId = ticket.Id,
                ClientId = ticket.Client.Id ?? 0,
                SupportUserId = ticket.SupportedBy.Id ?? 0,
                Rating = request.Rating,
                RatedAt = DateTime.UtcNow
            };

            _context.TicketRatings.Add(rating);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

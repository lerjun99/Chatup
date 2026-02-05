using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Handler
{
    public class UpdateTicketIsCaseCommandHandler : IRequestHandler<UpdateTicketIsCaseCommand, bool>
    {
        private readonly IChatDBContext _context;

        public UpdateTicketIsCaseCommandHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateTicketIsCaseCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null)
                return false;

            ticket.IsCase = false;
            ticket.Status = TicketStatus.Open;

            // Optional: track who updated it
            ticket.UpdatedBy = request.UpdatedBy;
            ticket.DateUpdated = DateTime.UtcNow;

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

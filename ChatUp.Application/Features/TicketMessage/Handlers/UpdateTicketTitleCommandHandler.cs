using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class UpdateTicketTitleCommandHandler : IRequestHandler<UpdateTicketTitleCommand, bool>
    {
        private readonly IChatDBContext _context;

        public UpdateTicketTitleCommandHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateTicketTitleCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _context.Tickets
                                       .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null)
                return false;

            ticket.IssueTitle = request.NewTitle;

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

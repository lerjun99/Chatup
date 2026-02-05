using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Handler
{
    public class RestoreTicketCommandHandler : IRequestHandler<RestoreTicketCommand, bool>
    {
        private readonly ITicketRepository _ticketRepository;

        public RestoreTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<bool> Handle(RestoreTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket == null || !ticket.IsArchived)
                return false; // Ticket not found or not archived

            ticket.IsArchived = false; // Restore
            await _ticketRepository.UpdateAsync(ticket, cancellationToken);

            return true;
        }
    }
}

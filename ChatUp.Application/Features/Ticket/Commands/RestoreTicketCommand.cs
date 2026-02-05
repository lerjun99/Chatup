using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Commands
{
    public class RestoreTicketCommand : IRequest<bool>
    {
        public int TicketId { get; }

        public RestoreTicketCommand(int ticketId)
        {
            TicketId = ticketId;
        }
    }
}

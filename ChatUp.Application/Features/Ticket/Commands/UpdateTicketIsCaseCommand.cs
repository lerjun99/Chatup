using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Commands
{
    public class UpdateTicketIsCaseCommand : IRequest<bool>
    {
        public int TicketId { get; set; }
        public int UpdatedBy { get; set; } // Optional: for auditing
    }
}

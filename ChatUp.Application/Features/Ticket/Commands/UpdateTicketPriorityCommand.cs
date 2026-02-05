using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Commands
{
    public class UpdateTicketPriorityCommand : IRequest<bool>
    {
        public int TicketId { get; set; }
        public TicketPriority NewPriority { get; set; }
        public int UpdatedBy { get; set; }
        public string Remarks { get; set; }
    }
}

using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Commands
{
    public record UpdateTicketStatusCommand(
            int TicketId,
        TicketStatus? NewStatus,
        TicketPriority? NewPriority,
        int UpdatedBy,
        DateTime UpdatedAt,
        string? Remarks = null
    ) : IRequest<bool>;
}

using ChatUp.Application.Features.Ticket.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Queries
{
    public class GetTicketDashboardSummaryQuery : IRequest<TicketDashboardDto>
    {
    }
}

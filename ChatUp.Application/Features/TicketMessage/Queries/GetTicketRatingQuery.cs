using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Queries
{
    public record GetTicketRatingQuery(int TicketId) : IRequest<int?>;
}

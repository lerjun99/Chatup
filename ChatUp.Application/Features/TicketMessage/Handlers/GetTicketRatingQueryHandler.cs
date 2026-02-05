using ChatUp.Application.Features.TicketMessage.Queries;
using ChatUp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class GetTicketRatingQueryHandler : IRequestHandler<GetTicketRatingQuery, int?>
    {
        private readonly ITicketRepository _ticketRepository;

        public GetTicketRatingQueryHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<int?> Handle(GetTicketRatingQuery request, CancellationToken cancellationToken)
        {
            var rating = await _ticketRepository.GetTicketRatingAsync(request.TicketId, cancellationToken);
            return rating;
        }
    }
}

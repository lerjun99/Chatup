using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Commands
{
    public class SubmitTicketRatingCommand : IRequest<bool>
    {
        public int TicketId { get; set; }
        public int Rating { get; set; }
        public int ClientId { get; set; } // who is rating
    }
}

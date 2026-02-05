using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Commands
{
    public record UpdateTicketTitleCommand(int TicketId, string NewTitle) : IRequest<bool>;

}

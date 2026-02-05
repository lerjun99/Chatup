using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Commands
{
    public class UpdateTicketStatusCommandValidator : AbstractValidator<UpdateTicketStatusCommand>
    {
        public UpdateTicketStatusCommandValidator()
        {
            RuleFor(x => x.TicketId).GreaterThan(0);
            RuleFor(x => x.NewStatus).IsInEnum();
            RuleFor(x => x.UpdatedBy).GreaterThan(0);
        }
    }
}

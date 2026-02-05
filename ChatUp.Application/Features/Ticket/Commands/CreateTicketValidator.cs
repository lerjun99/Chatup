using ChatUp.Application.Features.Ticket.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ChatUp.Application.Tickets.Commands.CreateTicket
{
    public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
    {
        public CreateTicketValidator()
        {
            RuleFor(x => x.IssueTitle).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.Status).NotEmpty();
        }
    }
}
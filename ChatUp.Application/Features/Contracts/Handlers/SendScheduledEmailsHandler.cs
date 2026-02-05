using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Contracts.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Handlers
{
    public class SendScheduledEmailsHandler : IRequestHandler<SendScheduledEmailsCommand>
    {
        private readonly IChatDBContext _context;
        private readonly IEmailService _emailService;

        public SendScheduledEmailsHandler(IChatDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task Handle(SendScheduledEmailsCommand request, CancellationToken cancellationToken)
        {
            var contracts = await _context.Contracts
                .Include(c => c.UserContracts)
                .Where(c => !c.IsTerminated)
                .ToListAsync(cancellationToken);

            foreach (var contract in contracts)
            {
                var daysUntilDue = (contract.ExpirationDate - request.CurrentDate)?.Days ?? 0;

                foreach (var user in contract.UserContracts)
                {
                    // Use the correct property EmailAddress from your UserAccount entity
                    if (string.IsNullOrEmpty(user.UserAccount.EmailAddress))
                        continue;

                    if (daysUntilDue == 30)
                        await _emailService.SendEmailAsync(user.UserAccount.EmailAddress!, $"Reminder: Contract {contract.Title} due in 30 days", "Please review the contract.");
                    else if (daysUntilDue == 15)
                        await _emailService.SendEmailAsync(user.UserAccount.EmailAddress!, $"Reminder: Contract {contract.Title} due in 15 days", "Please review the contract.");
                    else if (daysUntilDue == 0)
                        await _emailService.SendEmailAsync(user.UserAccount.EmailAddress!, $"Contract {contract.Title} Terminated Today", "The contract has reached its due date and is now terminated.");
                }
            }
        }
    }
}
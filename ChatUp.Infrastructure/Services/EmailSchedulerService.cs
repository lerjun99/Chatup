using ChatUp.Application.Features.Contracts.Commands;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class EmailSchedulerService : BackgroundService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EmailSchedulerService> _logger;

        public EmailSchedulerService(IMediator mediator, ILogger<EmailSchedulerService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _mediator.Send(new SendScheduledEmailsCommand());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending scheduled emails.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Runs once a day
            }
        }
    }
}

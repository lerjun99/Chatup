using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class ContractExpiryNotificationService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ContractExpiryNotificationService> _logger;
        public ContractExpiryNotificationService(IServiceProvider services, ILogger<ContractExpiryNotificationService> logger) 
        {
            _services = services;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // runs every day at a configurable time; simple approach: loop with 24h delay
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendNotifications(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending scheduled emails.");
                }


                // delay 24h
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }


        private async Task SendNotifications(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IChatDBContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();


            var today = DateTime.UtcNow.Date;
            var target30 = today.AddDays(30);
            var target15 = today.AddDays(15);


            // get contracts expiring on those dates
            var contracts = await ((DbContext)db).Set<Contract>()
            .Include(c => c.UserContracts).ThenInclude(uc => uc.UserAccount)
            .Where(c => c.ExpirationDate.HasValue && (
            c.ExpirationDate.Value.Date == target30 ||
            c.ExpirationDate.Value.Date == target15 ||
            c.ExpirationDate.Value.Date == today))
            .ToListAsync(cancellationToken);


            foreach (var contract in contracts)
            {
                var when = (contract.ExpirationDate.Value.Date == target30) ? "30 days" : (contract.ExpirationDate.Value.Date == target15) ? "15 days" : "today";
                var subject = $"Contract '{contract.Title}' expiry notice ({when})";
                var body = $"<p>Contract '<strong>{contract.Title}</strong>' will expire on {contract.ExpirationDate.Value:yyyy-MM-dd} ({when}).</p>";


                foreach (var uc in contract.UserContracts)
                {
                    var user = uc.UserAccount;
                    if (!string.IsNullOrEmpty(user.EmailAddress))
                    {
                        await emailService.SendEmailAsync(user.EmailAddress, subject, body);
                    }
                }
            }
        }
    }
}

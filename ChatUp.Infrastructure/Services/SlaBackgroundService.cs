using ChatUp.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatUp.Infrastructure.Services
{
    public class SlaBackgroundService : BackgroundService
    {
        private readonly ILogger<SlaBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SlaBackgroundService(
            ILogger<SlaBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ SLA Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();

                    // Do SLA work here
                    var breached = await repo.GetBreachedTicketsAsync();
                    foreach (var t in breached)
                    {
                        _logger.LogWarning($"⚠ Ticket {t.Id} breached SLA!");
                        // push updates via SignalR hub if needed
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SLA background task.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

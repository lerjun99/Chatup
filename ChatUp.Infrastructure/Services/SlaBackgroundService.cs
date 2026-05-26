using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatUp.Infrastructure.Services
{
    /// <summary>
    /// Runs on a 60-second interval and checks SLA status for all active tickets.
    /// SignalR notifications are sent ONLY when a ticket's status actually changes
    /// (OnTrack → Near → Breached), keeping UI re-renders to a minimum.
    /// </summary>
    public sealed class SlaBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly IHubContext<SlaHub> _hub;
        private readonly SlaStatusCache _cache;
        private readonly ILogger<SlaBackgroundService> _logger;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

        public SlaBackgroundService(
            IServiceProvider provider,
            IHubContext<SlaHub> hub,
            SlaStatusCache cache,
            ILogger<SlaBackgroundService> logger)
        {
            _provider = provider;
            _hub = hub;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Short startup delay so the host is fully ready before the first check
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAllTicketsAsync(stoppingToken);
                }
                catch (OperationCanceledException) { /* normal shutdown */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in SLA background check");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckAllTicketsAsync(CancellationToken ct)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IChatDBContext>();
            var calendarService = scope.ServiceProvider.GetRequiredService<IBusinessCalendarService>();

            // Only active tickets that have a due date
            var tickets = await db.Tickets
                .Where(t => t.DueDate.HasValue
                         && t.Status != TicketStatus.Closed
                         && t.Status != TicketStatus.Resolved)
                .ToListAsync(ct);

            if (tickets.Count == 0) return;

            var calendar = await calendarService.GetCalendarAsync("PH");
            var now = DateTime.UtcNow;

            var signalRTasks = new List<Task>();
            var newlyBreached = new List<Ticket>();

            foreach (var ticket in tickets)
            {
                var remaining = BusinessTimeHelper.GetBusinessTimeRemaining(
                    now, ticket.DueDate!.Value, calendar);

                var newStatus = remaining <= TimeSpan.Zero ? "Breached"
                    : remaining.TotalHours <= 2 ? "Near"
                    : "OnTrack";

                var time = remaining <= TimeSpan.Zero
                    ? "00:00:00"
                    : $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

                bool changed = _cache.TryDetectChange(ticket.Id, newStatus, out var prev);

                // Always keep cache current
                _cache.Set(ticket.Id, newStatus, time);

                if (changed)
                {
                    _logger.LogInformation(
                        "SLA status change — ticket {TicketId}: {Prev} → {New}",
                        ticket.Id, prev, newStatus);

                    // Capture loop variables for the closure
                    var capturedId = ticket.Id;
                    var capturedTime = time;
                    var capturedStatus = newStatus;

                    signalRTasks.Add(
                        _hub.Clients
                            .Group($"ticket-{capturedId}")
                            .SendAsync("ReceiveSlaUpdate", capturedId, capturedTime, capturedStatus, ct));
                }

                // Persist breach flag in DB (only once)
                if (newStatus == "Breached" && !ticket.IsBreached)
                {
                    ticket.IsBreached = true;
                    newlyBreached.Add(ticket);
                }
            }

            // Fire all SignalR pushes in parallel
            if (signalRTasks.Count > 0)
                await Task.WhenAll(signalRTasks);

            // Batch-persist any newly breached tickets
            if (newlyBreached.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "{Count} ticket(s) marked as SLA breached", newlyBreached.Count);
            }
        }
    }
}

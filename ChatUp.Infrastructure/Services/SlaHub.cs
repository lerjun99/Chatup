using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatUp.Infrastructure.Services
{
    /// <summary>
    /// SignalR hub for SLA status updates.
    /// Clients join ticket groups in a single batch call and receive their
    /// current SLA status immediately (served from the singleton cache,
    /// with a one-time DB fallback during the startup window).
    /// After that, updates arrive only when a status actually changes.
    /// </summary>
    public sealed class SlaHub : Hub
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SlaStatusCache _cache;

        public SlaHub(IServiceProvider serviceProvider, SlaStatusCache cache)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        /// <summary>
        /// Joins all provided ticket groups in one round-trip and immediately
        /// sends back each ticket current SLA status to the caller.
        /// </summary>
        public async Task JoinTicketGroups(List<int> ticketIds)
        {
            if (ticketIds == null || ticketIds.Count == 0) return;

            foreach (var id in ticketIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{id}");

            await SendInitialStatusAsync(ticketIds);
        }

        public async Task LeaveTicketGroup(string ticketId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        }

        private async Task SendInitialStatusAsync(List<int> ticketIds)
        {
            var cacheHits = new List<(int Id, string Time, string Status)>();
            var cacheMisses = new List<int>();

            foreach (var id in ticketIds)
            {
                var entry = _cache.Get(id);
                if (entry != null)
                    cacheHits.Add((id, entry.Time, entry.Status));
                else
                    cacheMisses.Add(id);
            }

            // Serve cache hits immediately - no DB involved
            foreach (var (id, time, status) in cacheHits)
                await Clients.Caller.SendAsync("ReceiveSlaUpdate", id, time, status);

            // DB fallback for anything not yet cached (startup window or new tickets)
            if (cacheMisses.Count > 0)
                await FetchAndSendFromDbAsync(cacheMisses);
        }

        private async Task FetchAndSendFromDbAsync(List<int> ticketIds)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IChatDBContext>();
                var calendarService = scope.ServiceProvider.GetRequiredService<IBusinessCalendarService>();

                // Single batch query for all cache-miss tickets
                var tickets = await db.Tickets
                    .Where(t => ticketIds.Contains(t.Id) && t.DueDate.HasValue)
                    .AsNoTracking()
                    .ToListAsync();

                if (tickets.Count == 0) return;

                var calendar = await calendarService.GetCalendarAsync("PH");
                var now = DateTime.UtcNow;

                foreach (var ticket in tickets)
                {
                    var remaining = BusinessTimeHelper.GetBusinessTimeRemaining(
                        now, ticket.DueDate!.Value, calendar);

                    var time = remaining <= TimeSpan.Zero
                        ? "00:00:00"
                        : $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

                    var status = remaining <= TimeSpan.Zero ? "Breached"
                        : remaining.TotalHours <= 2 ? "Near"
                        : "OnTrack";

                    // Populate cache so subsequent joins are served instantly
                    _cache.Set(ticket.Id, status, time);

                    await Clients.Caller.SendAsync("ReceiveSlaUpdate", ticket.Id, time, status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SlaHub] DB fallback error: {ex.Message}");
            }
        }
    }
}

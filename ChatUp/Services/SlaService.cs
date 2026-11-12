using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatUp.Application.Common.Helpers;
using ChatUp.Domain.Entities;

namespace ChatUp.Services
{
    public enum SlaState
    {
        None,       // ticket has no due date
        OnTrack,
        AtRisk,     // within the warning window
        Breached,   // active and past due
        Met,        // finished within SLA
        Missed      // finished after breaching
    }

    /// <summary>Point-in-time SLA reading for one ticket.</summary>
    public readonly record struct SlaSnapshot(
        SlaState State,
        TimeSpan Remaining,
        TimeSpan Overdue,
        double ElapsedPercent,
        bool IsPaused);

    /// <summary>
    /// Client-side SLA engine. Uses the same business calendar and the same
    /// BusinessTimeHelper as the server's SLA background service, so the live
    /// countdown pauses outside working hours, weekends and holidays exactly
    /// like the server's breach checks.
    /// </summary>
    public class SlaService
    {
        public const string DefaultRegion = "PH";

        // Same threshold the server uses for its "Near" status
        public static readonly TimeSpan AtRiskWindow = TimeSpan.FromHours(2);

        private static readonly HashSet<string> FinishedStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Resolved", "Closed", "Rejected" };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        // The calendar is global configuration: share it across circuits and refresh periodically
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);
        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static BusinessCalendar? _cachedCalendar;
        private static DateTime _cachedAtUtc;

        private readonly HttpClient _http;

        public SlaService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>Returns the SLA calendar, or null if it can't be loaded (timers then fall back to wall-clock time).</summary>
        public async Task<BusinessCalendar?> GetCalendarAsync()
        {
            if (_cachedCalendar != null && DateTime.UtcNow - _cachedAtUtc < CacheTtl)
                return _cachedCalendar;

            await CacheLock.WaitAsync();
            try
            {
                if (_cachedCalendar != null && DateTime.UtcNow - _cachedAtUtc < CacheTtl)
                    return _cachedCalendar;

                var dto = await _http.GetFromJsonAsync<SlaCalendarDto>(
                    $"{AppConfig.ChatUrl}Holidays/GetSlaCalendar/sla-calendar/{DefaultRegion}", JsonOptions);

                if (dto != null && dto.WorkStart < dto.WorkEnd && dto.WorkingDays.Count > 0)
                {
                    _cachedCalendar = new BusinessCalendar
                    {
                        Region = dto.Region ?? DefaultRegion,
                        WorkStart = dto.WorkStart,
                        WorkEnd = dto.WorkEnd,
                        WorkingDays = dto.WorkingDays,
                        Holidays = dto.Holidays.Select(h => h.Date.Date).ToList()
                    };
                    _cachedAtUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading SLA calendar: {ex.Message}");
            }
            finally
            {
                CacheLock.Release();
            }

            return _cachedCalendar;
        }

        public static bool IsFinished(string? status) =>
            status != null && FinishedStatuses.Contains(status);

        /// <summary>
        /// Evaluates SLA state at <paramref name="nowUtc"/>. All times are UTC, matching the server.
        /// </summary>
        public static SlaSnapshot Evaluate(
            DateTime receivedUtc,
            DateTime? dueUtc,
            string? status,
            bool breachedFlag,
            BusinessCalendar? calendar,
            DateTime nowUtc)
        {
            if (!dueUtc.HasValue || dueUtc.Value == default)
                return new SlaSnapshot(SlaState.None, TimeSpan.Zero, TimeSpan.Zero, 0, false);

            var due = dueUtc.Value;

            if (IsFinished(status))
            {
                return new SlaSnapshot(
                    breachedFlag ? SlaState.Missed : SlaState.Met,
                    TimeSpan.Zero, TimeSpan.Zero, 100, false);
            }

            var remaining = BusinessSpan(nowUtc, due, calendar);
            var total = BusinessSpan(receivedUtc, due, calendar);
            var paused = calendar != null && BusinessSpan(nowUtc, nowUtc.AddMinutes(1), calendar) == TimeSpan.Zero;

            // Past due is breached regardless of business hours: the due date is an absolute instant
            if (nowUtc >= due || remaining <= TimeSpan.Zero)
            {
                return new SlaSnapshot(
                    SlaState.Breached,
                    TimeSpan.Zero,
                    BusinessSpan(due, nowUtc, calendar),
                    100,
                    paused);
            }

            var elapsed = total > TimeSpan.Zero
                ? Math.Clamp((1 - remaining.TotalSeconds / total.TotalSeconds) * 100, 0, 100)
                : 0;

            // Active tickets follow the clock (like the server's GetById), so a due date
            // pushed out by a priority change is no longer shown as breached
            var state = remaining <= AtRiskWindow ? SlaState.AtRisk : SlaState.OnTrack;

            return new SlaSnapshot(state, remaining, TimeSpan.Zero, elapsed, paused);
        }

        private static TimeSpan BusinessSpan(DateTime from, DateTime to, BusinessCalendar? calendar)
        {
            if (to <= from)
                return TimeSpan.Zero;

            if (calendar != null)
            {
                try
                {
                    return BusinessTimeHelper.GetBusinessTimeRemaining(from, to, calendar);
                }
                catch (InvalidOperationException)
                {
                    // Misconfigured calendar: fall through to wall-clock time
                }
            }

            return to - from;
        }

        /// <summary>"02:14:09" under a day, "26h 14m" beyond (business time can span many calendar days).</summary>
        public static string FormatDuration(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;

            return span.TotalHours >= 24
                ? $"{(int)span.TotalHours}h {span.Minutes:D2}m"
                : $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        }

        private sealed class SlaCalendarDto
        {
            public string? Region { get; set; }
            public TimeSpan WorkStart { get; set; }
            public TimeSpan WorkEnd { get; set; }
            public List<DayOfWeek> WorkingDays { get; set; } = new();
            public List<HolidayItem> Holidays { get; set; } = new();
        }

        private sealed class HolidayItem
        {
            public DateTime Date { get; set; }
        }
    }

    /// <summary>
    /// One process-wide 1-second tick shared by every live SLA timer, instead of a
    /// timer per page that re-renders the whole page every second.
    /// Subscribers marshal back to their own circuit with InvokeAsync.
    /// </summary>
    public static class SlaTicker
    {
        private static readonly object Gate = new();
        private static Timer? _timer;
        private static event Action? TickInternal;
        private static int _subscribers;

        public static void Subscribe(Action handler)
        {
            lock (Gate)
            {
                TickInternal += handler;
                _subscribers++;
                _timer ??= new Timer(_ => Fire(), null, 1000, 1000);
            }
        }

        public static void Unsubscribe(Action handler)
        {
            lock (Gate)
            {
                TickInternal -= handler;
                _subscribers = Math.Max(0, _subscribers - 1);
                if (_subscribers == 0)
                {
                    _timer?.Dispose();
                    _timer = null;
                }
            }
        }

        private static void Fire()
        {
            Action? handlers;
            lock (Gate) handlers = TickInternal;
            if (handlers == null) return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch { /* one broken subscriber must not stop the others */ }
            }
        }
    }
}

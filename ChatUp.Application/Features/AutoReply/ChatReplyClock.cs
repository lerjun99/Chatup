namespace ChatUp.Application.Features.AutoReply
{
    /// <summary>
    /// The wall clock the auto-reply window ("HH:mm" to "HH:mm") is read against. DateTime.Now is the API host's own
    /// zone, which on a UTC server would put the window at the wrong hour. The zone is the "ChatTimeZone" setting,
    /// Philippine time (UTC+8) by default.
    /// </summary>
    public static class ChatReplyClock
    {
        private static readonly object Gate = new();
        private static (string? Id, TimeZoneInfo Zone)? _cached;

        public static DateTime LocalNow(string? zoneId) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Resolve(zoneId));

        /// <summary>
        /// True when <paramref name="now"/> falls inside the start-end window on one of the active days.
        /// Equal start and end means no window. An end before the start runs overnight (18:00-08:00), and the
        /// overnight tail after midnight belongs to the day the window started on.
        /// </summary>
        public static bool IsWithinWindow(string? start, string? end, string? activeDays, DateTime now)
        {
            if (!TimeSpan.TryParse(start, out var startTime) || !TimeSpan.TryParse(end, out var endTime))
                return false;

            if (startTime == endTime)
                return false;

            var days = ParseDays(activeDays);
            var time = now.TimeOfDay;

            // Times are saved as HH:mm, so the end minute counts in full (17:00 runs to 17:00:59)
            var endExclusive = endTime + TimeSpan.FromMinutes(1);

            if (startTime < endTime)
                return time >= startTime && time < endExclusive && DayActive(days, now.DayOfWeek);

            // Overnight window
            if (time >= startTime)
                return DayActive(days, now.DayOfWeek);

            if (time < endExclusive)
                return DayActive(days, now.AddDays(-1).DayOfWeek);

            return false;
        }

        public static HashSet<DayOfWeek> ParseDays(string? csv) => (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var d) && d is >= 0 and <= 6 ? (DayOfWeek?)d : null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet();

        private static bool DayActive(HashSet<DayOfWeek> days, DayOfWeek day) =>
            days.Count == 0 || days.Contains(day);

        private static TimeZoneInfo Resolve(string? zoneId)
        {
            lock (Gate)
            {
                if (_cached is { } cached && cached.Id == zoneId)
                    return cached.Zone;

                // IANA ids work on Linux (and Windows with ICU); the Windows id covers older hosts
                foreach (var id in new[] { zoneId, "Asia/Manila", "Singapore Standard Time" })
                {
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    try
                    {
                        var zone = TimeZoneInfo.FindSystemTimeZoneById(id);
                        _cached = (zoneId, zone);
                        return zone;
                    }
                    catch (TimeZoneNotFoundException) { }
                    catch (InvalidTimeZoneException) { }
                }

                // The Philippines has no daylight saving, so a fixed UTC+8 is exact
                var fixedZone = TimeZoneInfo.CreateCustomTimeZone("UTC+8", TimeSpan.FromHours(8), "UTC+8", "UTC+8");
                _cached = (zoneId, fixedZone);
                return fixedZone;
            }
        }
    }
}

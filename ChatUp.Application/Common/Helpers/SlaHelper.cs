using ChatUp.Domain.Entities;

namespace ChatUp.Application.Common.Helpers
{
    public static class SlaHelper
    {
        public static DateTime CalculateDueDate(
            TicketPriority priority,
            BusinessCalendar calendar,
            DateTime startDate)
        {
            var workHoursPerDay = (calendar.WorkEnd - calendar.WorkStart).TotalHours;

            var businessDuration = priority switch
            {
                TicketPriority.Critical => TimeSpan.FromHours(4),
                TicketPriority.High => TimeSpan.FromHours(12),
                TicketPriority.Medium => TimeSpan.FromHours(workHoursPerDay),
                TicketPriority.Low => TimeSpan.FromHours(workHoursPerDay * 3),
                _ => TimeSpan.FromHours(workHoursPerDay)
            };

            return AddBusinessTime(startDate, businessDuration, calendar);
        }

        private static DateTime AddBusinessTime(
            DateTime start,
            TimeSpan duration,
            BusinessCalendar calendar)
        {
            var current = NormalizeToBusinessTime(start, calendar);
            var remaining = duration;

            while (remaining > TimeSpan.Zero)
            {
                if (!IsWorkingDay(current, calendar))
                {
                    current = NextWorkingDay(current, calendar);
                    continue;
                }

                var workStart = current.Date.Add(calendar.WorkStart);
                var workEnd = current.Date.Add(calendar.WorkEnd);

                if (current < workStart)
                    current = workStart;

                if (current >= workEnd)
                {
                    current = NextWorkingDay(current, calendar);
                    continue;
                }

                var availableToday = workEnd - current;

                if (remaining <= availableToday)
                    return current.Add(remaining);

                remaining -= availableToday;
                current = NextWorkingDay(current, calendar);
            }

            return current;
        }

        private static DateTime NormalizeToBusinessTime(DateTime date, BusinessCalendar calendar)
        {
            if (!IsWorkingDay(date, calendar))
                return NextWorkingDay(date, calendar);

            var workStart = date.Date.Add(calendar.WorkStart);
            var workEnd = date.Date.Add(calendar.WorkEnd);

            if (date < workStart)
                return workStart;

            if (date >= workEnd)
                return NextWorkingDay(date, calendar);

            return date;
        }

        // ✅ SAFE HOLIDAY + WEEKEND CHECK
        private static bool IsWorkingDay(DateTime date, BusinessCalendar calendar)
        {
            var workingDays = calendar.WorkingDays ?? new List<DayOfWeek>();
            var holidays = calendar.Holidays ?? new List<DateTime>();

            bool isWorkingDay = workingDays.Contains(date.DayOfWeek);
            bool isHoliday = holidays.Any(h => h.Date == date.Date);

            return isWorkingDay && !isHoliday;
        }

        private static DateTime NextWorkingDay(DateTime date, BusinessCalendar calendar)
        {
            var next = date.Date.AddDays(1);

            while (!IsWorkingDay(next, calendar))
            {
                next = next.AddDays(1);
            }

            return next.Date.Add(calendar.WorkStart);
        }

        // =============================
        // SLA STATUS
        // =============================

        public static bool CheckBreach(Ticket ticket)
        {
            if (!ticket.DueDate.HasValue)
                return false;

            if (ticket.Status == TicketStatus.Resolved ||
                ticket.Status == TicketStatus.Closed)
            {
                return ticket.ResolvedDate.HasValue &&
                       ticket.ResolvedDate.Value > ticket.DueDate.Value;
            }

            return DateTime.UtcNow > ticket.DueDate.Value;
        }

        // ✅ FIXED: safer formatting
        public static string FormatRemainingTime(DateTime? dueDate)
        {
            if (!dueDate.HasValue)
                return "--:--:--";

            var remaining = dueDate.Value - DateTime.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return "00:00:00";

            return $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        public static string GetSlaStatus(DateTime? dueDate)
        {
            if (!dueDate.HasValue)
                return "Unknown";

            var remaining = dueDate.Value - DateTime.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return "Breached";

            if (remaining.TotalHours <= 2)
                return "Near";

            return "OnTrack";
        }
    }
}
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
namespace ChatUp.Application.Common.Helpers
{
    public static class BusinessTimeHelper
    {
        public static TimeSpan GetBusinessTimeRemaining(
            DateTime start,
            DateTime end,
            BusinessCalendar calendar)
        {
            if (calendar == null)
                throw new ArgumentNullException(nameof(calendar));

            if (calendar.WorkingDays == null || calendar.Holidays == null)
                throw new InvalidOperationException("BusinessCalendar configuration is incomplete.");

            if (calendar.WorkStart < TimeSpan.Zero || calendar.WorkEnd < TimeSpan.Zero)
                throw new InvalidOperationException("Invalid WorkStart/WorkEnd (negative TimeSpan).");

            if (calendar.WorkStart >= TimeSpan.FromDays(1) ||
                calendar.WorkEnd >= TimeSpan.FromDays(1))
                throw new InvalidOperationException("WorkStart/WorkEnd must be within 24 hours.");

            if (calendar.WorkStart >= calendar.WorkEnd)
                throw new InvalidOperationException("WorkStart must be earlier than WorkEnd.");

            if (start >= end)
                return TimeSpan.Zero;

            TimeSpan total = TimeSpan.Zero;
            var current = NormalizeStart(start, calendar);

            while (current < end)
            {
                if (current == DateTime.MaxValue)
                    break;

                if (!IsWorkingDay(current, calendar))
                {
                    current = NextWorkingDay(current, calendar);
                    continue;
                }

                var workStart = SafeAddTime(current.Date, calendar.WorkStart);
                var workEnd = SafeAddTime(current.Date, calendar.WorkEnd);

                // Align to working window
                if (current < workStart)
                    current = workStart;

                if (current >= workEnd)
                {
                    current = NextWorkingDay(current, calendar);
                    continue;
                }

                var segmentEnd = end < workEnd ? end : workEnd;

                if (segmentEnd > current)
                    total += segmentEnd - current;

                current = segmentEnd;
            }

            return total;
        }

        // =========================
        // Normalize start position
        // =========================
        private static DateTime NormalizeStart(DateTime date, BusinessCalendar cal)
        {
            if (!IsWorkingDay(date, cal))
                return NextWorkingDay(date, cal);

            var workStart = SafeAddTime(date.Date, cal.WorkStart);
            var workEnd = SafeAddTime(date.Date, cal.WorkEnd);

            if (date < workStart)
                return workStart;

            if (date >= workEnd)
                return NextWorkingDay(date, cal);

            return date;
        }

        // =========================
        // Working day check
        // =========================
        private static bool IsWorkingDay(DateTime date, BusinessCalendar cal)
        {
            if (cal == null)
                return false;

            // already in-memory collections (NO JSON)
            var workingDays = cal.WorkingDays?.Select(x => x.ToString()).ToList()
                              ?? new List<string>();

            var holidays = cal.Holidays?.Select(x => x.Date).ToList()
                            ?? new List<DateTime>();

            bool isWorkingDay =
                workingDays.Contains(date.DayOfWeek.ToString()) &&
                !holidays.Contains(date.Date);

            return isWorkingDay;
        }

        // =========================
        // Next valid working day
        // =========================
        private static DateTime NextWorkingDay(DateTime date, BusinessCalendar cal)
        {
            var nextDate = date.Date;

            // prevent overflow
            if (nextDate >= DateTime.MaxValue.Date)
                return DateTime.MaxValue;

            nextDate = nextDate.AddDays(1);

            while (!IsWorkingDay(nextDate, cal))
            {
                if (nextDate >= DateTime.MaxValue.Date)
                    return DateTime.MaxValue;

                nextDate = nextDate.AddDays(1);
            }

            return SafeAddTime(nextDate, cal.WorkStart);
        }

        // =========================
        // Safe DateTime builder
        // =========================
        private static DateTime SafeAddTime(DateTime date, TimeSpan time)
        {
            try
            {
                var result = date.Date.Add(time);

                if (result < DateTime.MinValue || result > DateTime.MaxValue)
                    throw new InvalidOperationException("Date overflow detected in SLA calculation.");

                return result;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidOperationException(
                    "Invalid DateTime operation in SLA engine. Check calendar configuration.",
                    ex);
            }
        }
    }
}

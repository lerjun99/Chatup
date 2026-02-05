using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Helpers
{
    public static class SlaHelper
    {
        public static DateTime CalculateDueDate(TicketPriority priority)
        {
            return priority switch
            {
                TicketPriority.Critical => DateTime.UtcNow.AddHours(4),
                TicketPriority.High => DateTime.UtcNow.AddHours(12),
                TicketPriority.Medium => DateTime.UtcNow.AddDays(1),
                TicketPriority.Low => DateTime.UtcNow.AddDays(3),
                _ => DateTime.UtcNow.AddDays(1)
            };
        }

        public static bool CheckBreach(Ticket ticket)
        {
            if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
            {
                return ticket.ResolvedDate.HasValue && ticket.ResolvedDate > ticket.DueDate;
            }
            return DateTime.UtcNow > ticket.DueDate;
        }

        public static string FormatRemainingTime(DateTime dueDate)
        {
            var remaining = dueDate - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) return "⚠ SLA Breached!";
            return $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
    }
}

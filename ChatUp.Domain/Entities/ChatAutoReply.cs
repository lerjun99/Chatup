using System;
using System.ComponentModel.DataAnnotations;

namespace ChatUp.Domain.Entities
{
    /// <summary>
    /// Automatic replies sent to a client who writes in a ticket chat (modelled on the AFL chat auto reply).
    /// One row per client; the row with ClientId = null is the default for every client without its own row.
    /// </summary>
    public class ChatAutoReply
    {
        public int Id { get; set; }

        /// <summary>The client this row applies to. Null = default for all clients.</summary>
        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public bool IsEnabled { get; set; } = true;

        /// <summary>Sent on a client message once support has been quiet for the idle period. Empty = none.</summary>
        [MaxLength(2000)]
        public string? InstantMessage { get; set; }

        /// <summary>Start of the scheduled window, "HH:mm" on the chat clock. Equal start and end = no window.</summary>
        [MaxLength(5)]
        public string StartTime { get; set; } = "00:00";

        /// <summary>End of the scheduled window, "HH:mm". End before start = overnight window (e.g. 18:00-08:00).</summary>
        [MaxLength(5)]
        public string EndTime { get; set; } = "00:00";

        /// <summary>Days the window applies on, as comma-separated DayOfWeek numbers (0 = Sunday). Empty = every day.</summary>
        [MaxLength(20)]
        public string? ActiveDays { get; set; }

        /// <summary>Sent on every client message that arrives inside the window (the "away" reply). Empty = none.</summary>
        [MaxLength(2000)]
        public string? ScheduledMessage { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}

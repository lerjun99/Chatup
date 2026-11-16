using System;
using System.ComponentModel.DataAnnotations;

namespace ChatUp.Domain.Entities
{
    /// <summary>A bell notification for one user (ticket filed, new reply, status change...).</summary>
    public class UserNotification
    {
        public long Id { get; set; }

        /// <summary>Recipient.</summary>
        public int UserId { get; set; }

        /// <summary>See <c>NotificationTypes</c>: TicketCreated, TicketReply, TicketStatus.</summary>
        [MaxLength(40)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public int? TicketId { get; set; }

        /// <summary>Who caused it (not notified themselves).</summary>
        public int? ActorUserId { get; set; }

        /// <summary>Unread replies on the same ticket are folded into one notification; this counts them.</summary>
        public int Count { get; set; } = 1;

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}

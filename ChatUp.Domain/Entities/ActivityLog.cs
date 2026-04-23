using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatUp.Domain.Entities
{
    public enum ActivityType
    {
        TicketCreated = 1,
        TicketStatusChanged = 2,
        TicketPriorityChanged = 3,
        TicketMessageSent = 4,
        TicketUploadAdded = 5,
        TicketUploadRemoved = 6
    }

    public class ActivityLog
    {
        public int Id { get; set; }

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int? ActorUserId { get; set; }
        public UserAccount? ActorUser { get; set; }

        public ActivityType ActivityType { get; set; }

        [MaxLength(300)]
        public string Summary { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? DetailsJson { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}

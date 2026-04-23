using System;

namespace ChatUp.Application.Features.Activity.DTOs
{
    public class ActivityLogDto
    {
        public int Id { get; set; }
        public int? TicketId { get; set; }
        public string? TicketNo { get; set; }
        public int? ActorUserId { get; set; }
        public string ActorName { get; set; } = "System";
        public string ActivityType { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
    }
}

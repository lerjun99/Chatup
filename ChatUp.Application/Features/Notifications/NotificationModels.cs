using MediatR;

namespace ChatUp.Application.Features.Notifications
{
    public static class NotificationTypes
    {
        public const string TicketCreated = "TicketCreated";
        public const string TicketReply = "TicketReply";
        public const string TicketStatus = "TicketStatus";
    }

    public class NotificationDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? TicketId { get; set; }
        public string? TicketNo { get; set; }

        /// <summary>The ticket's status now (for the inbox link).</summary>
        public string? TicketStatus { get; set; }
        public string? ActorName { get; set; }
        public int Count { get; set; } = 1;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Set on realtime pushes: the recipient's unread total after this notification.</summary>
        public int UnreadCount { get; set; }
    }

    public class NotificationListDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int UnreadCount { get; set; }
        public bool HasMore { get; set; }
    }

    /// <summary>Latest notifications of a user; <paramref name="BeforeId"/> pages back through older ones.</summary>
    public record GetNotificationsQuery(int UserId, int Take = 20, long? BeforeId = null, bool UnreadOnly = false) : IRequest<NotificationListDto>;

    /// <summary>Marks one notification read (only the recipient's own). Returns the new unread count.</summary>
    public record MarkNotificationReadCommand(long Id, int UserId) : IRequest<int>;

    /// <summary>Marks every notification of the user read, or only those of one ticket.</summary>
    public record MarkAllNotificationsReadCommand(int UserId, int? TicketId = null) : IRequest<int>;
}

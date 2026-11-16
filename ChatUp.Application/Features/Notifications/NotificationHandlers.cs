using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.Notifications
{
    public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, NotificationListDto>
    {
        private readonly IChatDBContext _db;

        public GetNotificationsHandler(IChatDBContext db) => _db = db;

        public async Task<NotificationListDto> Handle(GetNotificationsQuery request, CancellationToken ct)
        {
            var take = Math.Clamp(request.Take, 1, 50);

            var query = _db.UserNotifications.AsNoTracking().Where(n => n.UserId == request.UserId);
            if (request.UnreadOnly)
                query = query.Where(n => !n.IsRead);
            if (request.BeforeId.HasValue)
                query = query.Where(n => n.Id < request.BeforeId.Value);

            var rows = await query
                .OrderByDescending(n => n.Id)
                .Take(take + 1)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.TicketId,
                    Ticket = _db.Tickets.Where(t => t.Id == n.TicketId).Select(t => new { t.TicketNo, t.Status }).FirstOrDefault(),
                    ActorName = _db.UserAccounts.Where(u => u.Id == n.ActorUserId).Select(u => u.FullName).FirstOrDefault(),
                    n.Count,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync(ct);

            var items = rows.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                TicketId = n.TicketId,
                TicketNo = n.Ticket?.TicketNo,
                TicketStatus = (n.Ticket?.Status ?? TicketStatus.Open).ToString(),
                ActorName = n.ActorName,
                Count = n.Count,
                IsRead = n.IsRead,
                CreatedAt = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
            }).ToList();

            var unread = await _db.UserNotifications.CountAsync(n => n.UserId == request.UserId && !n.IsRead, ct);

            return new NotificationListDto
            {
                Items = items.Take(take).ToList(),
                HasMore = items.Count > take,
                UnreadCount = unread
            };
        }
    }

    public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, int>
    {
        private readonly IChatDBContext _db;

        public MarkNotificationReadHandler(IChatDBContext db) => _db = db;

        public async Task<int> Handle(MarkNotificationReadCommand request, CancellationToken ct)
        {
            var row = await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == request.UserId, ct);
            if (row != null && !row.IsRead)
            {
                row.IsRead = true;
                row.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return await _db.UserNotifications.CountAsync(n => n.UserId == request.UserId && !n.IsRead, ct);
        }
    }

    public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
    {
        private readonly IChatDBContext _db;

        public MarkAllNotificationsReadHandler(IChatDBContext db) => _db = db;

        public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
        {
            var query = _db.UserNotifications.Where(n => n.UserId == request.UserId && !n.IsRead);
            if (request.TicketId.HasValue)
                query = query.Where(n => n.TicketId == request.TicketId);

            // One UPDATE statement, whatever the number of rows
            var now = DateTime.UtcNow;
            await query.ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now), ct);

            return await _db.UserNotifications.CountAsync(n => n.UserId == request.UserId && !n.IsRead, ct);
        }
    }
}

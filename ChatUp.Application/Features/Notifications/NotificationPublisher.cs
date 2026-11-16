using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatUp.Application.Features.Notifications
{
    /// <summary>
    /// Raises bell notifications for ticket events. Every call returns at once: recipients are worked out, rows saved
    /// and pushes sent in the background, so the request that caused the event is never slowed down.
    /// </summary>
    public interface INotificationPublisher
    {
        /// <summary>A ticket was filed: the client's support agents and the project's developers are told.</summary>
        void TicketCreated(int ticketId, int? actorUserId);

        /// <summary>A chat message: a client message tells support, a support reply tells the client.</summary>
        void TicketReply(int ticketId, int senderUserId, bool fromClient, string preview);

        /// <summary>Status / priority / project changed: the requester, support agents and developers are told.</summary>
        void TicketUpdated(int ticketId, int actorUserId, string summary);
    }

    public sealed class NotificationPublisher : INotificationPublisher
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationPublisher> _logger;

        public NotificationPublisher(IServiceScopeFactory scopeFactory, ILogger<NotificationPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void TicketCreated(int ticketId, int? actorUserId) =>
            Run(ticketId, async (ctx, t) =>
            {
                var recipients = await ctx.StaffForTicketAsync(t, includeDevelopers: true);
                if (t.RequestedById.HasValue) recipients.Add(t.RequestedById.Value);

                // Who filed it: staff filing for a client are the ticket's agent; a client filing a case is the requester
                if (!actorUserId.HasValue)
                {
                    var supporterIsStaff = t.SupportedById.HasValue && await ctx.Db.UserAccounts.AsNoTracking()
                        .AnyAsync(u => u.Id == t.SupportedById && (u.UserType == TicketAudience.Admin ||
                                       u.UserType == TicketAudience.Support || u.UserType == TicketAudience.Developer));
                    actorUserId = supporterIsStaff ? t.SupportedById : t.RequestedById;
                }

                var what = t.IsCase ? "case" : "ticket";
                await ctx.SendAsync(recipients, actorUserId, NotificationTypes.TicketCreated,
                    title: $"New {what} filed{(string.IsNullOrEmpty(t.ClientName) ? "" : $" · {t.ClientName}")}",
                    message: $"{t.Number} — {t.IssueTitle}",
                    t.Id);
            });

        public void TicketReply(int ticketId, int senderUserId, bool fromClient, string preview) =>
            Run(ticketId, async (ctx, t) =>
            {
                HashSet<int> recipients;

                if (fromClient)
                {
                    // Every support agent of the client (and the agent on the ticket) should know the client wrote
                    recipients = await ctx.StaffForTicketAsync(t, includeDevelopers: false);
                }
                else
                {
                    // Support answered: the requester, else every account of the client
                    recipients = new HashSet<int>();
                    if (t.RequestedById.HasValue)
                        recipients.Add(t.RequestedById.Value);
                    else if (t.ClientId.HasValue)
                        recipients.UnionWith(await ctx.Db.UserAccounts.AsNoTracking()
                            .Where(u => u.ClientId == t.ClientId && u.IsDeleted == 0 && u.Id.HasValue)
                            .Select(u => u.Id!.Value).ToListAsync());
                }

                var senderName = await ctx.Db.UserAccounts.AsNoTracking()
                    .Where(u => u.Id == senderUserId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "Someone";

                var text = string.IsNullOrWhiteSpace(preview) ? "📎 Sent an attachment" : preview.Trim();
                if (text.Length > 160) text = text[..160] + "…";

                await ctx.SendAsync(recipients, senderUserId, NotificationTypes.TicketReply,
                    title: fromClient ? $"{senderName} replied on {t.Number}" : $"Support replied on {t.Number}",
                    message: fromClient ? text : $"{senderName}: {text}",
                    t.Id,
                    foldUnreadReplies: true);
            }, ticketChanged: false);

        public void TicketUpdated(int ticketId, int actorUserId, string summary) =>
            Run(ticketId, async (ctx, t) =>
            {
                var recipients = await ctx.StaffForTicketAsync(t, includeDevelopers: true);
                if (t.RequestedById.HasValue) recipients.Add(t.RequestedById.Value);

                await ctx.SendAsync(recipients, actorUserId, NotificationTypes.TicketStatus,
                    title: $"{t.Number} updated · {Label(t.Status)}",
                    message: string.IsNullOrWhiteSpace(summary) ? t.IssueTitle : summary,
                    t.Id);
            });

        private static string Label(TicketStatus? s) => s switch
        {
            TicketStatus.InProgress => "In progress",
            null => "Open",
            _ => s.ToString()!
        };

        /// <param name="ticketChanged">
        /// True for new tickets and status changes: inbox lists, badges and dashboards refresh. False for chat replies,
        /// which already reach the inbox live as messages: only dashboards refresh.
        /// </param>
        private void Run(int ticketId, Func<Context, TicketInfo, Task> work, bool ticketChanged = true)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var ctx = new Context(
                        scope.ServiceProvider.GetRequiredService<IChatDBContext>(),
                        scope.ServiceProvider.GetRequiredService<IChatHubContext>());

                    var ticket = await ctx.Db.Tickets.AsNoTracking()
                        .Where(t => t.Id == ticketId)
                        .Select(t => new TicketInfo
                        {
                            Id = t.Id,
                            Number = t.IsCase ? t.CaseNo : t.TicketNo,
                            IsCase = t.IsCase,
                            IssueTitle = t.IssueTitle,
                            ClientId = t.ClientId,
                            ClientName = t.Client != null ? t.Client.ClientName : null,
                            ProjectId = t.ProjectId,
                            RequestedById = t.RequestedById,
                            SupportedById = t.SupportedById,
                            Status = t.Status
                        })
                        .FirstOrDefaultAsync();

                    if (ticket == null)
                        return;

                    await work(ctx, ticket);

                    if (ticketChanged)
                        await ctx.Hub.NotifyTicketUpdated();
                    else
                        await ctx.Hub.NotifyDashboardChanged();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification failed for ticket {TicketId}", ticketId);
                }
            });
        }

        private sealed class TicketInfo
        {
            public int Id { get; set; }
            public string Number { get; set; } = string.Empty;
            public bool IsCase { get; set; }
            public string IssueTitle { get; set; } = string.Empty;
            public int? ClientId { get; set; }
            public string? ClientName { get; set; }
            public int? ProjectId { get; set; }
            public int? RequestedById { get; set; }
            public int? SupportedById { get; set; }
            public TicketStatus? Status { get; set; }
        }

        private sealed class Context
        {
            public Context(IChatDBContext db, IChatHubContext hub)
            {
                Db = db;
                Hub = hub;
            }

            public IChatDBContext Db { get; }
            public IChatHubContext Hub { get; }

            /// <summary>Support agents assigned to the client, the agent on the ticket and (optionally) the project's developers.</summary>
            public async Task<HashSet<int>> StaffForTicketAsync(TicketInfo t, bool includeDevelopers)
            {
                var ids = new HashSet<int>();

                if (t.ClientId.HasValue)
                {
                    ids.UnionWith(await Db.UserClientAssignments.AsNoTracking()
                        .Where(a => a.ClientId == t.ClientId && a.UserId.HasValue && a.User != null &&
                                    a.User.IsDeleted == 0 && a.User.UserType == TicketAudience.Support)
                        .Select(a => a.UserId!.Value)
                        .ToListAsync());
                }

                if (includeDevelopers && t.ProjectId.HasValue)
                {
                    ids.UnionWith(await Db.UserProjects.AsNoTracking()
                        .Where(p => p.ProjectId == t.ProjectId && p.UserAccount.IsDeleted == 0)
                        .Select(p => p.UserAccountId)
                        .ToListAsync());
                }

                if (t.SupportedById.HasValue)
                {
                    var isStaff = await Db.UserAccounts.AsNoTracking()
                        .AnyAsync(u => u.Id == t.SupportedById && u.IsDeleted == 0 &&
                                       (u.UserType == TicketAudience.Admin || u.UserType == TicketAudience.Support || u.UserType == TicketAudience.Developer));
                    if (isStaff)
                        ids.Add(t.SupportedById.Value);
                }

                return ids;
            }

            public async Task SendAsync(HashSet<int> recipients, int? actorUserId, string type, string title, string message,
                int ticketId, bool foldUnreadReplies = false)
            {
                if (actorUserId.HasValue)
                    recipients.Remove(actorUserId.Value);

                if (recipients.Count == 0)
                    return;

                title = Cut(title, 200);
                message = Cut(message, 500);
                var now = DateTime.UtcNow;
                var ids = recipients.ToList();

                // Unread replies on the same ticket become one notification ("3 new replies") instead of a flood
                var existing = foldUnreadReplies
                    ? await Db.UserNotifications
                        .Where(n => ids.Contains(n.UserId) && !n.IsRead && n.Type == type && n.TicketId == ticketId)
                        .ToListAsync()
                    : new List<UserNotification>();

                var rows = new List<UserNotification>();
                foreach (var userId in ids)
                {
                    var row = existing.FirstOrDefault(n => n.UserId == userId);
                    if (row != null)
                    {
                        row.Count += 1;
                        row.Title = Cut($"{row.Count} new replies on {title[(title.LastIndexOf(' ') + 1)..]}", 200);
                        row.Message = message;
                        row.ActorUserId = actorUserId;
                        row.CreatedAt = now;
                    }
                    else
                    {
                        row = new UserNotification
                        {
                            UserId = userId,
                            Type = type,
                            Title = title,
                            Message = message,
                            TicketId = ticketId,
                            ActorUserId = actorUserId,
                            CreatedAt = now
                        };
                        Db.UserNotifications.Add(row);
                    }
                    rows.Add(row);
                }

                await Db.SaveChangesAsync();

                // Unread totals for every recipient in one grouped query, so each badge is exact
                var unread = await Db.UserNotifications.AsNoTracking()
                    .Where(n => ids.Contains(n.UserId) && !n.IsRead)
                    .GroupBy(n => n.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);

                var ticket = await Db.Tickets.AsNoTracking().Where(t => t.Id == ticketId)
                    .Select(t => new { t.TicketNo, t.Status }).FirstOrDefaultAsync();
                var actorName = actorUserId.HasValue
                    ? await Db.UserAccounts.AsNoTracking().Where(u => u.Id == actorUserId).Select(u => u.FullName).FirstOrDefaultAsync()
                    : null;

                foreach (var row in rows)
                {
                    await Hub.SendNotificationToUser(row.UserId, new NotificationDto
                    {
                        Id = row.Id,
                        Type = row.Type,
                        Title = row.Title,
                        Message = row.Message,
                        TicketId = row.TicketId,
                        TicketNo = ticket?.TicketNo,
                        TicketStatus = (ticket?.Status ?? TicketStatus.Open).ToString(),
                        ActorName = actorName,
                        Count = row.Count,
                        IsRead = false,
                        CreatedAt = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
                        UnreadCount = unread.GetValueOrDefault(row.UserId)
                    });
                }
            }

            private static string Cut(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
        }
    }
}

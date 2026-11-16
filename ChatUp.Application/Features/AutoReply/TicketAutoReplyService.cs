using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatUp.Application.Features.AutoReply
{
    public interface ITicketAutoReplyService
    {
        /// <summary>
        /// Queues the auto replies owed for a client message that was just saved. Returns at once: the replies are
        /// worked out and sent in the background, so the client's own send is never slowed down.
        /// </summary>
        void QueueForClientMessage(int ticketId, int messageId, DateTime sentAtUtc);
    }

    /// <summary>
    /// Same rules as the AFL chat auto reply:
    ///  • Instant reply — sent on a client message once support has been quiet in the ticket for
    ///    <see cref="StaffIdleBeforeInstantReply"/> (so the first message always gets it, an active chat never does).
    ///  • Scheduled reply — sent on every client message that arrives inside the scheduled window (e.g. after hours).
    /// A client's own settings win over the default; a disabled client row switches auto replies off for that client.
    /// </summary>
    public sealed class TicketAutoReplyService : ITicketAutoReplyService
    {
        // Lets the client's own message land (response + realtime echo) before the reply appears under it
        private static readonly TimeSpan DeliveryDelay = TimeSpan.FromSeconds(2);

        // The instant reply is held back while support is active in the ticket
        public static readonly TimeSpan StaffIdleBeforeInstantReply = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TicketAutoReplyService> _logger;

        public TicketAutoReplyService(IServiceScopeFactory scopeFactory, ILogger<TicketAutoReplyService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void QueueForClientMessage(int ticketId, int messageId, DateTime sentAtUtc)
        {
            // Fire-and-forget on purpose; runs in its own DI scope because the request's DbContext is disposed
            // as soon as the client's response is sent
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DeliveryDelay);
                    await SendAsync(ticketId, messageId, sentAtUtc);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto reply failed for ticket {TicketId}, message {MessageId}", ticketId, messageId);
                }
            });
        }

        private async Task SendAsync(int ticketId, int messageId, DateTime sentAtUtc)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IChatDBContext>();
            var hub = scope.ServiceProvider.GetRequiredService<IChatHubContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var ticket = await db.Tickets
                .AsNoTracking()
                .Where(t => t.Id == ticketId)
                .Select(t => new { t.Id, t.ClientId, t.SupportedById, t.Status })
                .FirstOrDefaultAsync();

            if (ticket == null || ticket.Status is TicketStatus.Closed or TicketStatus.Rejected)
                return;

            // The client's own row, else the default row
            var rule = await db.ChatAutoReplies
                .AsNoTracking()
                .Where(x => x.ClientId == ticket.ClientId || x.ClientId == null)
                .OrderByDescending(x => x.ClientId.HasValue)
                .FirstOrDefaultAsync();

            if (rule == null || !rule.IsEnabled)
                return;

            var replies = new List<string>();

            if (!string.IsNullOrWhiteSpace(rule.InstantMessage))
            {
                var lastStaffMessageAt = await db.TicketMessages
                    .AsNoTracking()
                    .Where(m => m.TicketId == ticketId && !m.IsUser && m.Id != messageId)
                    .MaxAsync(m => m.DateCreated);

                if (lastStaffMessageAt == null || sentAtUtc - lastStaffMessageAt.Value >= StaffIdleBeforeInstantReply)
                    replies.Add(rule.InstantMessage);
            }

            if (!string.IsNullOrWhiteSpace(rule.ScheduledMessage))
            {
                var now = ChatReplyClock.LocalNow(config["ChatTimeZone"]);
                if (ChatReplyClock.IsWithinWindow(rule.StartTime, rule.EndTime, rule.ActiveDays, now))
                    replies.Add(rule.ScheduledMessage);
            }

            if (replies.Count == 0)
                return;

            var senderId = await ResolveSenderAsync(db, ticket.ClientId, ticket.SupportedById);
            if (senderId == null)
            {
                _logger.LogWarning("Auto reply skipped for ticket {TicketId}: no support or admin account to send it as", ticketId);
                return;
            }

            var audience = await TicketAudience.GetUserIdsAsync(db, ticketId, CancellationToken.None);

            // One after another, so the instant reply always lands before the scheduled one
            foreach (var text in replies)
            {
                var message = new Domain.Entities.TicketMessage
                {
                    TicketId = ticketId,
                    SenderId = senderId.Value,
                    IsUser = false,
                    IsAutoReply = true,
                    Content = text,
                    DateCreated = DateTime.UtcNow
                };

                db.TicketMessages.Add(message);
                await db.SaveChangesAsync();

                var dto = await db.TicketMessages
                    .AsNoTracking()
                    .Where(m => m.Id == message.Id)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        TicketId = m.TicketId,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FullName,
                        IsUser = false,
                        IsAutoReply = true,
                        Content = m.Content,
                        DateCreated = m.DateCreated,
                        SenderAvatar = "images/default.png"
                    })
                    .FirstAsync();

                await hub.SendTicketMessageToConversation(ticketId, dto);
                await hub.SendTicketMessageToUsers(audience, dto);
            }
        }

        /// <summary>
        /// Auto replies are saved as support messages, so they need a staff account behind them: the agent on the
        /// ticket, else a support agent assigned to the client, else an admin.
        /// </summary>
        private static async Task<int?> ResolveSenderAsync(IChatDBContext db, int? clientId, int? supportedById)
        {
            if (supportedById.HasValue)
            {
                var isStaff = await db.UserAccounts.AsNoTracking()
                    .AnyAsync(u => u.Id == supportedById && u.IsDeleted == 0 &&
                                   (u.UserType == TicketAudience.Admin || u.UserType == TicketAudience.Support));
                if (isStaff)
                    return supportedById;
            }

            if (clientId.HasValue)
            {
                var supportId = await db.UserClientAssignments.AsNoTracking()
                    .Where(a => a.ClientId == clientId && a.User != null && a.User.IsDeleted == 0 && a.User.UserType == TicketAudience.Support)
                    .OrderBy(a => a.Id)
                    .Select(a => a.UserId)
                    .FirstOrDefaultAsync();
                if (supportId.HasValue)
                    return supportId;
            }

            return await db.UserAccounts.AsNoTracking()
                .Where(u => u.IsDeleted == 0 && u.UserType == TicketAudience.Admin)
                .OrderBy(u => u.Id)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }
    }
}

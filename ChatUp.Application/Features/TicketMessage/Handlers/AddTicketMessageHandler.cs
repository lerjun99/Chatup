using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.AutoReply;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class AddTicketMessageHandler : IRequestHandler<AddTicketMessageCommand, MessageDto>
    {
        private readonly IChatDBContext _context;
        private readonly IChatHubContext _chatHub;
        private readonly ITicketAutoReplyService _autoReply;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AddTicketMessageHandler> _logger;
        private readonly Notifications.INotificationPublisher _notifications;

        public AddTicketMessageHandler(
            IChatDBContext context,
            IChatHubContext chatHub,
            ITicketAutoReplyService autoReply,
            IServiceScopeFactory scopeFactory,
            ILogger<AddTicketMessageHandler> logger,
            Notifications.INotificationPublisher notifications)
        {
            _context = context;
            _chatHub = chatHub;
            _autoReply = autoReply;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _notifications = notifications;
        }

        public async Task<MessageDto> Handle(AddTicketMessageCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            // -------------------------------
            // 1. SENDER + TICKET (two light reads)
            // -------------------------------
            var sender = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => u.Id == request.SenderId)
                .Select(u => new
                {
                    u.FullName,
                    u.UserType,
                    Avatar = _context.UploadedFile
                        .Where(f => f.UserAccountId == u.Id && f.FileType == "ProfileImg")
                        .OrderByDescending(f => f.Id)
                        .Select(f => f.Base64Content)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
                ?? throw new KeyNotFoundException($"Ticket {request.TicketId} was not found.");

            // The side of the conversation comes from the sender's account, not from the caller: admins used to be
            // sent as the client side, which counted their replies as client messages
            var isClientSide = sender != null ? !TicketAudience.IsStaff(sender.UserType) : request.IsUser;

            // -------------------------------
            // 2. MESSAGE + ATTACHMENTS + ACTIVITY + TICKET STATE — saved together
            // -------------------------------
            var message = new Domain.Entities.TicketMessage
            {
                TicketId = request.TicketId,
                SenderId = request.SenderId,
                IsUser = isClientSide,
                Content = request.Content ?? string.Empty,
                DateCreated = now
            };

            _context.TicketMessages.Add(message);

            _context.ActivityLogs.Add(new ActivityLog
            {
                TicketId = request.TicketId,
                ActorUserId = request.SenderId,
                ActivityType = ActivityType.TicketMessageSent,
                Summary = string.IsNullOrWhiteSpace(request.Content) ? "Sent an attachment" : "Sent a message",
                OccurredAtUtc = now
            });

            foreach (var file in request.Attachments ?? Enumerable.Empty<TicketUploadDto>())
            {
                var thumbnail = file.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                    ? ImageCompressionHelper.CreateThumbnail(file.Base64Content)
                    : null;

                message.TicketUploads.Add(new TicketUpload
                {
                    TicketId = request.TicketId,
                    UploadedById = request.SenderId,
                    FileName = file.FileName,
                    FileType = file.FileType,
                    Base64Content = file.Base64Content,
                    ThumbnailBase64 = thumbnail,
                    DateUploaded = now
                });

                _context.ActivityLogs.Add(new ActivityLog
                {
                    TicketId = request.TicketId,
                    ActorUserId = request.SenderId,
                    ActivityType = ActivityType.TicketUploadAdded,
                    Summary = $"Uploaded {file.FileName}",
                    OccurredAtUtc = now
                });
            }

            var emailClient = false;

            if (isClientSide)
            {
                ticket.LastClientMessageAt = now;
                // A new client message allows a new support reminder and resets the reply notification
                ticket.SupportResponseReminderSent = false;
                ticket.SupportResponseReminderSentAt = null;
                ticket.HasUnreadSupportReply = false;
            }
            else
            {
                ticket.LastSupportReplyAt = now;
                ticket.FirstResponseAt ??= now;
                ticket.SupportResponseReminderSent = false;
                ticket.SupportResponseReminderSentAt = null;

                // One email per unread streak of support replies, not one per message
                if (!ticket.HasUnreadSupportReply)
                {
                    ticket.HasUnreadSupportReply = true;
                    emailClient = true;
                }
            }

            // Interaction: one row per sender per ticket (the old lookup never matched, adding a row per message)
            var interaction = await _context.TicketInteractions
                .FirstOrDefaultAsync(i => i.TicketId == request.TicketId && i.SenderId == request.SenderId, cancellationToken);

            if (interaction == null)
            {
                _context.TicketInteractions.Add(new TicketInteraction
                {
                    TicketId = request.TicketId,
                    SenderId = request.SenderId,
                    ReceiverId = request.SenderId,
                    LastMessageTime = now,
                    TicketMessage = message
                });
            }
            else
            {
                interaction.LastMessageTime = now;
                interaction.TicketMessage = message;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // -------------------------------
            // 3. DTO built in memory — thumbnails only, full files load on demand
            // -------------------------------
            var dto = new MessageDto
            {
                Id = message.Id,
                TicketId = message.TicketId,
                SenderId = message.SenderId,
                SenderName = sender?.FullName ?? "Unknown",
                SenderAvatar = string.IsNullOrEmpty(sender?.Avatar) ? "images/default.png" : sender.Avatar,
                IsUser = message.IsUser,
                IsCase = message.IsCase,
                Content = message.Content,
                DateCreated = message.DateCreated,
                Attachments = message.TicketUploads.Select(u => new TicketUploadDto
                {
                    Id = u.Id,
                    TicketId = u.TicketId,
                    TicketMessageId = message.Id,
                    FileName = u.FileName,
                    FileType = u.FileType,
                    ThumbnailBase64 = u.ThumbnailBase64,
                    UploadedById = u.UploadedById,
                    DateUploaded = u.DateUploaded
                }).ToList()
            };

            // -------------------------------
            // 4. REAL-TIME: the open conversation + every support agent of the client, wherever they are
            // -------------------------------
            try
            {
                var audience = await TicketAudience.GetUserIdsAsync(_context, request.TicketId, cancellationToken);
                await _chatHub.SendTicketMessageToConversation(message.TicketId, dto);
                await _chatHub.SendTicketMessageToUsers(audience, dto);
            }
            catch (Exception ex)
            {
                // The message is saved; a failed broadcast must not fail the send
                _logger.LogWarning(ex, "Realtime delivery failed for ticket message {MessageId}", message.Id);
            }

            // -------------------------------
            // 5. BACKGROUND WORK — never delays the sender
            // -------------------------------
            if (isClientSide)
                _autoReply.QueueForClientMessage(request.TicketId, message.Id, now);

            // Bell: client message → the client's support agents; support reply → the client
            _notifications.TicketReply(request.TicketId, request.SenderId, isClientSide, message.Content);

            if (emailClient && ticket.RequestedById.HasValue)
                QueueClientEmail(ticket.RequestedById.Value, ticket.TicketNo, sender?.FullName, message.Content);

            return dto;
        }

        private void QueueClientEmail(int requesterId, string ticketNo, string? senderName, string content)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IChatDBContext>();
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var clientEmail = await db.UserAccounts
                        .AsNoTracking()
                        .Where(u => u.Id == requesterId)
                        .Select(u => u.EmailAddress)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrWhiteSpace(clientEmail))
                        return;

                    var body = $@"
                        <div style='font-family: Arial, sans-serif; background-color: #f4f6f8; padding: 20px;'>
                            <div style='max-width: 500px; margin: auto; background: #ffffff; border-radius: 10px; padding: 20px; text-align: center;'>
                                <h2 style='color: #1877f2;'>ChatUp</h2>
                                <p><strong>{WebUtility.HtmlEncode(senderName ?? "Support")}</strong> responded to your ticket</p>
                                <p style='font-size:12px;color:#999;'>{DateTime.Now:MMM dd, hh:mm tt}</p>
                                <div style='background:#f1f3f5;padding:15px;border-radius:8px;margin:20px 0;white-space:pre-wrap;'>
                                    {WebUtility.HtmlEncode(content)}
                                </div>
                                <a href='https://portal.odeccisolutions.com/'
                                   style='background:#1877f2;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;'>
                                   View Message
                                </a>
                            </div>
                        </div>";

                    await email.SendEmailAsync(clientEmail, $"Ticket #{ticketNo} Updated", body, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ticket reply email failed for ticket {TicketNo}", ticketNo);
                }
            });
        }
    }
}

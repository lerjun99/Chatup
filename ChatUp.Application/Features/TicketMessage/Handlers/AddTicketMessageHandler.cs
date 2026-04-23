using AutoMapper;
using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class AddTicketMessageHandler : IRequestHandler<AddTicketMessageCommand, MessageDto>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;
        private readonly IChatHubContext _chatHub; // Not IHubContext<ChatHub>
        private readonly IEmailService _emailService;
        public AddTicketMessageHandler(IChatDBContext context, IMapper mapper, IChatHubContext chatHub, IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _chatHub = chatHub;
            _emailService = emailService;
        }
  
              public async Task<MessageDto> Handle(AddTicketMessageCommand request, CancellationToken cancellationToken)
        {
            // -------------------------------
            // 1. CREATE MESSAGE
            // -------------------------------
            var message = new ChatUp.Domain.Entities.TicketMessage
            {
                TicketId = request.TicketId,
                SenderId = request.SenderId,
                IsUser = request.IsUser,
                Content = request.Content ?? string.Empty,
                DateCreated = DateTime.UtcNow
            };

            _context.TicketMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);

            // -------------------------------
            // 2. ACTIVITY LOG (MESSAGE)
            // -------------------------------
            _context.ActivityLogs.Add(new ActivityLog
            {
                TicketId = message.TicketId,
                ActorUserId = request.SenderId,
                ActivityType = ActivityType.TicketMessageSent,
                Summary = string.IsNullOrWhiteSpace(request.Content)
                    ? "Sent an attachment"
                    : "Sent a message",
                OccurredAtUtc = message.DateCreated ?? DateTime.UtcNow
            });

            // -------------------------------
            // 3. ATTACHMENTS
            // -------------------------------
            if (request.Attachments?.Any() == true)
            {
                foreach (var file in request.Attachments)
                {
                    string? thumbnail = null;

                    if (file.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    {
                        thumbnail = ImageCompressionHelper.CreateThumbnail(file.Base64Content);
                    }

                    _context.TicketUploads.Add(new TicketUpload
                    {
                        TicketId = message.TicketId,
                        TicketMessageId = message.Id,
                        UploadedById = request.SenderId,
                        FileName = file.FileName,
                        FileType = file.FileType,
                        Base64Content = file.Base64Content,
                        ThumbnailBase64 = thumbnail,
                        DateUploaded = DateTime.UtcNow
                    });

                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        TicketId = message.TicketId,
                        ActorUserId = request.SenderId,
                        ActivityType = ActivityType.TicketUploadAdded,
                        Summary = $"Uploaded {file.FileName}",
                        OccurredAtUtc = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            // -------------------------------
            // 4. RESPONSE TRACKING + EMAIL
            // -------------------------------
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket != null)
            {
                bool isSupport = !request.IsUser;

                if (isSupport)
                {
                    ticket.LastSupportReplyAt = message.DateCreated;

                    // ✅ Prevent duplicate email spam
                    if (!ticket.HasUnreadSupportReply)
                    {
                        ticket.HasUnreadSupportReply = true;

                        // 🔍 Get client email from Users table
                        var clientEmail = await _context.UserAccounts
                            .Where(u => u.Id == ticket.RequestedById)
                            .Select(u => u.EmailAddress)
                            .FirstOrDefaultAsync(cancellationToken);
                        var emailBody = $@"
                        <div style='font-family: Arial, sans-serif; background-color: #f4f6f8; padding: 20px;'>
                            <div style='max-width: 500px; margin: auto; background: #ffffff; border-radius: 10px; padding: 20px; text-align: center;'>

                                <h2 style='color: #1877f2;'>ChatUp</h2>

                                <p><strong>{message.Sender?.FullName ?? "Support"}</strong> responded to your ticket</p>

                                <p style='font-size:12px;color:#999;'>{DateTime.Now:MMM dd, hh:mm tt}</p>

                                <div style='background:#f1f3f5;padding:15px;border-radius:8px;margin:20px 0;'>
                                    {message.Content}
                                </div>

                                <a href='https://portal.odeccisolutions.com/'
                                   style='background:#1877f2;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;'>
                                   View Message
                                </a>

                            </div>
                        </div>";
                        if (!string.IsNullOrEmpty(clientEmail))
                        {
                            await _emailService.SendEmailAsync(
                            clientEmail,
                            $"Ticket #{ticket.TicketNo} Updated",
                            emailBody,
                            true
                        );
                        }
                       }
                }
                else
                {
                    // Client message
                    ticket.LastClientMessageAt = message.DateCreated;

                    // Optional: reset flag when client replies
                    ticket.HasUnreadSupportReply = false;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            // -------------------------------
            // 5. INTERACTION TRACKING
            // -------------------------------
            var interaction = await _context.TicketInteractions
                .FirstOrDefaultAsync(i =>
                    i.TicketId == request.TicketId &&
                    i.SenderId == request.SenderId &&
                    i.ReceiverId != request.SenderId,
                    cancellationToken);

            if (interaction == null)
            {
                interaction = new TicketInteraction
                {
                    TicketId = request.TicketId,
                    SenderId = request.SenderId,
                    ReceiverId = request.SenderId,
                    LastMessageTime = message.DateCreated ?? DateTime.UtcNow,
                    TicketMessage = message
                };

                _context.TicketInteractions.Add(interaction);
            }
            else
            {
                interaction.LastMessageTime = message.DateCreated ?? DateTime.UtcNow;
                interaction.TicketMessage = message;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // -------------------------------
            // 6. MAP DTO
            // -------------------------------
            var dto = await _context.TicketMessages
                .AsNoTracking()
                .Where(m => m.Id == message.Id)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    TicketId = m.TicketId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    IsUser = m.IsUser,
                    Content = m.Content,
                    DateCreated = m.DateCreated,
                    IsCase = m.IsCase,
                    SenderAvatar = m.Sender.Uploads
                        .Select(x => x.Base64Content)
                        .FirstOrDefault() ?? "images/default.png",
                    Attachments = m.TicketUploads
                        .Where(u => !u.IsDeleted)
                        .Select(u => new TicketUploadDto
                        {
                            Id = u.Id,
                            FileName = u.FileName,
                            FileType = u.FileType,
                            Base64Content = u.Base64Content,
                            ThumbnailBase64 = u.ThumbnailBase64
                        }).ToList()
                })
                .FirstAsync(cancellationToken);

            // -------------------------------
            // 7. REAL-TIME BROADCAST
            // -------------------------------
            await _chatHub.SendTicketMessageToConversation(
                message.TicketId,
                dto);

            return dto;
        }
    }
}

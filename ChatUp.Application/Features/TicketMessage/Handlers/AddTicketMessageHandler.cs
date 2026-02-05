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

        public AddTicketMessageHandler(IChatDBContext context, IMapper mapper, IChatHubContext chatHub)
        {
            _context = context;
            _mapper = mapper;
            _chatHub = chatHub;
        }
        public async Task<MessageDto> Handle(AddTicketMessageCommand request, CancellationToken cancellationToken)
        {
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

            // Attachments
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
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            // 🔁 Update or create interaction
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
                    ReceiverId = request.SenderId,      // user receives
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

            // ✅ FULL DTO (matches LoadTicketConversation)
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

            // ✅ Broadcast AFTER everything is ready
            await _chatHub.SendTicketMessageToConversation(
                message.TicketId,
                dto);

            return dto;

        }
    }
}

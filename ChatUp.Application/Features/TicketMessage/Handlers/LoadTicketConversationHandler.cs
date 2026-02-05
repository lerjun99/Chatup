using AutoMapper;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using DocumentFormat.OpenXml.Spreadsheet;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class LoadTicketConversationHandler : IRequestHandler<LoadTicketConversationQuery, TicketConversationDto>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;

        public LoadTicketConversationHandler(IChatDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TicketConversationDto> Handle(LoadTicketConversationQuery request, CancellationToken ct)
        {
            int page = Math.Max(1, request.PageNumber);
            int pageSize = Math.Max(1, Math.Min(100, request.PageSize)); // reasonable upper bound

            // ───────────────────────────────────────────────
            // 1. Fetch only the needed message IDs + core fields (very light)
            //    Ordered DESC by Id → newest first
            // ───────────────────────────────────────────────
            var messageKeys = await _context.TicketMessages
                .AsNoTracking()
                .Where(m => m.TicketId == request.TicketId)
                .OrderByDescending(m => m.Id)           // or m.DateCreated if you prefer
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.DateCreated,
                    m.IsUser,
                    m.IsCase
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            if (!messageKeys.Any())
            {
                return new TicketConversationDto
                {
                    Messages = new(),
                    ActiveUsers = new()
                };
            }

            var messageIds = messageKeys.Select(k => k.Id).ToList();

            // ───────────────────────────────────────────────
            // 2. Fetch messages + sender info + avatar in ONE query
            //    Using projection → minimal data transfer
            // ───────────────────────────────────────────────
            var messagesWithSender = await _context.TicketMessages
                .AsNoTracking()
                .Where(m => messageIds.Contains(m.Id))
                .Select(m => new
                {
                    m.Id,
                    m.TicketId,
                    m.SenderId,
                    m.IsUser,
                    m.Content,
                    m.DateCreated,
                    m.IsCase,
                    SenderFullName = m.Sender.FullName,
                    SenderAvatarBase64 = m.Sender.Uploads
                      .OrderByDescending(u => u.Id)
                      .Select(u => u.Base64Content)
                      .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.Id, ct);

            // ───────────────────────────────────────────────
            // 3. Fetch ALL relevant attachments in ONE query
            // ───────────────────────────────────────────────
            var allAttachments = await _context.TicketUploads
                .AsNoTracking()
                .Where(u => u.TicketId == request.TicketId &&
                           (messageIds.Contains(u.TicketMessageId ?? 0) || u.TicketMessageId == null))
                .Select(u => new
                {
                    u.Id,
                    u.TicketMessageId,
                    u.FileName,
                    u.FileType,
                    u.Base64Content,
                    u.ThumbnailBase64,
                    u.IsDeleted,
                    u.DateUploaded,
                    u.UploadedById
                })
                .ToListAsync(ct);

            // Split in memory (small dataset → acceptable)
            var msgAttachments = allAttachments
                .Where(a => a.TicketMessageId.HasValue && messageIds.Contains(a.TicketMessageId.Value) && !a.IsDeleted)
                .GroupBy(a => a.TicketMessageId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(a => new TicketUploadDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileType = a.FileType,
                        Base64Content = a.Base64Content,
                        ThumbnailBase64 = a.ThumbnailBase64,
                        UploadedById = a.UploadedById
                    }).ToList());

            var removedFileNamesByMsg = allAttachments
                .Where(a => a.TicketMessageId.HasValue && messageIds.Contains(a.TicketMessageId.Value) && a.IsDeleted)
                .GroupBy(a => a.TicketMessageId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(a => a.FileName).ToList());

            var orphanUploads = allAttachments
                .Where(a => !a.TicketMessageId.HasValue && !a.IsDeleted)
                .ToList();

            // ───────────────────────────────────────────────
            // 4. Build MessageDto list (newest → oldest)
            // ───────────────────────────────────────────────
            var messagesDto = new List<MessageDto>(messageKeys.Count + (orphanUploads.Any() && page == 1 ? 1 : 0));

            foreach (var key in messageKeys)
            {
                var msgData = messagesWithSender[key.Id];

                var content = msgData.Content ?? "";
                if (removedFileNamesByMsg.TryGetValue(key.Id, out var removed) && removed.Count > 0)
                {
                    content += $"\nAttachment removed: {string.Join(", ", removed)}";
                }

                var dto = new MessageDto
                {
                    Id = key.Id,
                    TicketId = msgData.TicketId,
                    SenderId = msgData.SenderId,
                    SenderName = msgData.SenderFullName ?? "Unknown",
                    IsUser = msgData.IsUser,
                    Content = content,
                    SenderAvatar = msgData.SenderAvatarBase64 ?? "images/default.png",
                    DateCreated = msgData.DateCreated,
                    IsCase = msgData.IsCase,
                    Attachments = msgAttachments.GetValueOrDefault(key.Id, new List<TicketUploadDto>())
                };

                messagesDto.Add(dto);
            }

            // Reverse to oldest → newest (if frontend expects ascending order)
            messagesDto.Reverse();

            // ───────────────────────────────────────────────
            // 5. Optional: prepend orphan/system message on page 1
            // ───────────────────────────────────────────────
            if (page == 1 && orphanUploads.Any())
            {
                var systemMsg = new MessageDto
                {
                    Id = 0,
                    TicketId = request.TicketId,
                    SenderId = 0,
                    SenderName = "System",
                    IsUser = false,
                    Content = "Orphan attachments:\n" + string.Join("\n", orphanUploads.Select(u => u.FileName)),
                    DateCreated = orphanUploads.Min(u => u.DateUploaded),
                    IsCase = false,
                    Attachments = orphanUploads.Select(u => new TicketUploadDto
                    {
                        Id = u.Id,
                        FileName = u.FileName,
                        FileType = u.FileType,
                        Base64Content = u.Base64Content,
                        ThumbnailBase64 = u.ThumbnailBase64
                    }).ToList()
                };

                messagesDto.Insert(0, systemMsg);
            }

            // ───────────────────────────────────────────────
            // 6. Active users – better to derive only from fetched page (lighter)
            //    If you truly need ALL-time active users → separate endpoint/query
            // ───────────────────────────────────────────────
            var senderIdsInPage = messageKeys.Select(k => k.SenderId).Distinct().ToList();

            var activeUsers = await _context.UserAccounts // assuming you have DbSet<ApplicationUser> Users
                .AsNoTracking()
                .Where(u => senderIdsInPage.Contains(u.Id ?? 0))
                .Select(u => new ActiveUserDto
                {
                    UserId = u.Id ?? 0,
                    UserName = u.FullName ?? "Unknown"
                })
                .ToListAsync(ct);

            // Optional: enrich LastMessageTime from the page if you want approximate
            var lastTimes = messageKeys
                .GroupBy(k => k.SenderId)
                .ToDictionary(g => g.Key, g => g.Max(k => k.DateCreated));

            //foreach (var user in activeUsers)
            //{
            //    if (lastTimes.TryGetValue(user.UserId, out var time))
            //        user.LastMessageTime = time;
            //}

            return new TicketConversationDto
            {
                Messages = messagesDto,
                ActiveUsers = activeUsers
            };
        }
    }
}

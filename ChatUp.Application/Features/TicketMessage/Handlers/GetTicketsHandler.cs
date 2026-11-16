using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class GetTicketsHandler : IRequestHandler<GetTicketsQueries, TicketPagedResponse>
    {
        private const int PreviewLength = 140;
        private const int MaxPageSize = 100;

        private readonly IChatDBContext _context;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<GetTicketsHandler> _logger;

        public GetTicketsHandler(IChatDBContext context, IUserRepository userRepo, ILogger<GetTicketsHandler> logger)
        {
            _context = context;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<TicketPagedResponse> Handle(GetTicketsQueries request, CancellationToken cancellationToken)
        {
            try
            {
                var page = Math.Max(1, request.Page);
                var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

                // 1️⃣ Who may see what, then the filters every count shares
                var scoped = await ApplyUserFilterAsync(_context.Tickets.AsNoTracking(), request.UserId, cancellationToken);
                scoped = ApplySharedFilters(scoped, request);

                // 2️⃣ Counts per status (closed included) in ONE query — the tabs keep their numbers while switching
                var counts = await scoped
                    .GroupBy(t => t.Status)
                    .Select(g => new TicketStatusCountDto
                    {
                        Status = g.Key ?? TicketStatus.Open,
                        Count = g.Count(),
                        NeedsReplyCount = g.Count(t => t.LastClientMessageAt != null &&
                                                       (t.LastSupportReplyAt == null || t.LastSupportReplyAt < t.LastClientMessageAt))
                    })
                    .ToListAsync(cancellationToken);

                // Several null statuses can fall into Open; merge so each status appears once
                var statusCounts = counts
                    .GroupBy(c => c.Status)
                    .Select(g => new TicketStatusCountDto { Status = g.Key, Count = g.Sum(x => x.Count), NeedsReplyCount = g.Sum(x => x.NeedsReplyCount) })
                    .ToList();

                var needsReplyCount = statusCounts
                    .Where(c => c.Status is not (TicketStatus.Closed or TicketStatus.Rejected))
                    .Sum(c => c.NeedsReplyCount);

                // Total for the current filter worked out from the counts — no extra COUNT query
                var inView = request.StatusFilter.HasValue
                    ? statusCounts.Where(c => c.Status == request.StatusFilter.Value)
                    : statusCounts.Where(c => c.Status != TicketStatus.Closed);
                var totalCount = request.NeedsReply ? inView.Sum(c => c.NeedsReplyCount) : inView.Sum(c => c.Count);

                // 3️⃣ Status / needs-reply view
                var query = ApplyViewFilters(scoped, request);

                // 4️⃣ Page — ordered on ticket columns (no per-row aggregate over all messages)
                var rows = await ApplySort(query, request.Sort)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        Dto = new TicketDto
                        {
                            Id = t.Id,
                            TicketNo = t.TicketNo,
                            CaseNo = t.CaseNo,
                            IsCase = t.IsCase,
                            IssueTitle = t.IssueTitle,
                            ProjectId = t.ProjectId,
                            ProjectTitle = t.Project != null ? t.Project.Title : string.Empty,
                            Status = (t.Status ?? TicketStatus.Open).ToString(),
                            DateReceived = t.DateReceived,
                            DueDate = t.DueDate,
                            Priority = t.Priority,
                            IsArchived = t.IsArchived,
                            RequestedById = t.RequestedById ?? 0,
                            RequestedByName = t.RequestedBy != null ? t.RequestedBy.FullName : string.Empty,
                            ClientId = t.ClientId ?? 0,
                            ClientName = t.Client != null ? t.Client.ClientName : string.Empty,
                            ClientAvatar = t.Client != null ? t.Client.PhotoUrl ?? string.Empty : string.Empty,
                            ClientEmail = t.Client != null ? t.Client.EmailAddress ?? string.Empty : string.Empty,
                            SupportedById = t.SupportedById ?? 0,
                            SupportedByName = t.SupportedBy != null ? t.SupportedBy.FullName : string.Empty,
                            LastClientMessageAt = t.LastClientMessageAt,
                            NeedsReply = t.LastClientMessageAt != null &&
                                         (t.LastSupportReplyAt == null || t.LastSupportReplyAt < t.LastClientMessageAt)
                        },
                        t.LastSupportReplyAt,
                        // Uses IX_TicketMessages_TicketId_DateCreated: one seek per ticket on the page
                        LastMessageId = _context.TicketMessages
                            .Where(m => m.TicketId == t.Id)
                            .OrderByDescending(m => m.DateCreated)
                            .ThenByDescending(m => m.Id)
                            .Select(m => (int?)m.Id)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                var tickets = rows.Select(r => r.Dto).ToList();

                if (tickets.Count > 0)
                {
                    var ticketIds = tickets.Select(t => t.Id).ToList();

                    // 5️⃣ Last-message previews for the page in one query
                    var lastIds = rows.Where(r => r.LastMessageId.HasValue).Select(r => r.LastMessageId!.Value).ToList();
                    var previews = lastIds.Count == 0
                        ? new Dictionary<int, PreviewRow>()
                        : await _context.TicketMessages
                            .AsNoTracking()
                            .Where(m => lastIds.Contains(m.Id))
                            .Select(m => new PreviewRow
                            {
                                TicketId = m.TicketId,
                                Text = m.Content.Length > PreviewLength ? m.Content.Substring(0, PreviewLength) : m.Content,
                                SenderName = m.Sender.FullName,
                                SenderId = m.SenderId,
                                IsUser = m.IsUser,
                                IsAutoReply = m.IsAutoReply,
                                DateCreated = m.DateCreated,
                                HasAttachment = m.TicketUploads.Any(u => !u.IsDeleted)
                            })
                            .ToDictionaryAsync(p => p.TicketId, cancellationToken);

                    var now = DateTime.UtcNow;

                    foreach (var row in rows)
                    {
                        var t = row.Dto;

                        if (previews.TryGetValue(t.Id, out var p))
                        {
                            t.LastMessagePreview = p.Text;
                            t.LastMessageSenderName = p.SenderName;
                            t.LastMessageSenderId = p.SenderId;
                            t.LastMessageFromClient = p.IsUser;
                            t.LastMessageIsAutoReply = p.IsAutoReply;
                            t.LastMessageHasAttachment = p.HasAttachment;
                        }

                        t.LastActivityAt = Latest(p?.DateCreated, t.LastClientMessageAt, row.LastSupportReplyAt, t.DateReceived);

                        if (t.LastActivityAt.HasValue)
                        {
                            var span = now - t.LastActivityAt.Value;
                            t.ResponseTimeSeconds = (long)Math.Max(0, span.TotalSeconds);
                            t.ResponseTime = Humanize(span);
                        }
                        else
                        {
                            t.ResponseTimeSeconds = long.MaxValue;
                            t.ResponseTime = "No messages yet";
                        }
                    }

                    // 6️⃣ Optional extras (other pages still use them; the inbox turns them off)
                    if (request.IncludeMessages)
                        await LoadMessagesAsync(tickets, ticketIds, cancellationToken);

                    if (request.IncludeHistories)
                        await LoadHistoriesAsync(tickets, ticketIds, cancellationToken);
                }

                return new TicketPagedResponse
                {
                    Tickets = tickets,
                    StatusCounts = statusCounts,
                    NeedsReplyCount = needsReplyCount,
                    TotalCount = totalCount,
                    HasMore = page * pageSize < totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTickets failed for user {UserId}", request.UserId);
                return new TicketPagedResponse();
            }
        }

        private sealed class PreviewRow
        {
            public int TicketId { get; set; }
            public string Text { get; set; } = string.Empty;
            public string? SenderName { get; set; }
            public int SenderId { get; set; }
            public bool IsUser { get; set; }
            public bool IsAutoReply { get; set; }
            public DateTime? DateCreated { get; set; }
            public bool HasAttachment { get; set; }
        }

        #region Filters

        /// <summary>Filters shared by the list and the status counts: archive, search, priority, client, mine.</summary>
        private static IQueryable<Domain.Entities.Ticket> ApplySharedFilters(
            IQueryable<Domain.Entities.Ticket> query, GetTicketsQueries request)
        {
            query = request.IncludeArchived
                ? query.Where(t => t.IsArchived)
                : query.Where(t => !t.IsArchived);

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.IssueTitle.Contains(search) ||
                    t.TicketNo.Contains(search) ||
                    t.CaseNo.Contains(search) ||
                    (t.Client != null && t.Client.ClientName.Contains(search)) ||
                    (t.RequestedBy != null && t.RequestedBy.FullName.Contains(search)));
            }

            if (request.PriorityFilter.HasValue)
                query = query.Where(t => t.Priority == request.PriorityFilter.Value);

            if (request.ClientId is > 0)
                query = query.Where(t => t.ClientId == request.ClientId);

            if (request.AssignedToMe)
                query = query.Where(t => t.SupportedById == request.UserId);

            return query;
        }

        /// <summary>The status tab and the needs-reply toggle (not part of the counts).</summary>
        private static IQueryable<Domain.Entities.Ticket> ApplyViewFilters(
            IQueryable<Domain.Entities.Ticket> query, GetTicketsQueries request)
        {
            query = request.StatusFilter.HasValue
                ? query.Where(t => t.Status == request.StatusFilter.Value)
                : query.Where(t => t.Status != TicketStatus.Closed);

            if (request.NeedsReply)
                query = query.Where(t => t.LastClientMessageAt != null &&
                                         (t.LastSupportReplyAt == null || t.LastSupportReplyAt < t.LastClientMessageAt));

            return query;
        }

        private static IQueryable<Domain.Entities.Ticket> ApplySort(IQueryable<Domain.Entities.Ticket> query, string? sort)
        {
            switch (sort?.Trim().ToLowerInvariant())
            {
                case "waiting":
                    // Longest-waiting client first; tickets without a client message last
                    return query
                        .OrderBy(t => t.LastClientMessageAt == null)
                        .ThenBy(t => t.LastClientMessageAt)
                        .ThenBy(t => t.Id);

                case "priority":
                    return query
                        .OrderByDescending(t => t.Priority)
                        .ThenByDescending(t =>
                            t.LastClientMessageAt != null && (t.LastSupportReplyAt == null || t.LastClientMessageAt > t.LastSupportReplyAt)
                                ? t.LastClientMessageAt
                                : (t.LastSupportReplyAt ?? (DateTime?)t.DateReceived))
                        .ThenByDescending(t => t.Id);

                case "due":
                    return query
                        .OrderBy(t => t.DueDate == null)
                        .ThenBy(t => t.DueDate)
                        .ThenByDescending(t => t.Id);

                default:
                    // Most recent activity (client or support message, else the date received)
                    return query
                        .OrderByDescending(t =>
                            t.LastClientMessageAt != null && (t.LastSupportReplyAt == null || t.LastClientMessageAt > t.LastSupportReplyAt)
                                ? t.LastClientMessageAt
                                : (t.LastSupportReplyAt ?? (DateTime?)t.DateReceived))
                        .ThenByDescending(t => t.Id);
            }
        }

        private async Task<IQueryable<Domain.Entities.Ticket>> ApplyUserFilterAsync(
            IQueryable<Domain.Entities.Ticket> query,
            int userId,
            CancellationToken ct)
        {
            if (userId <= 0)
                return query.Where(_ => false);

            var user = await _userRepo.Query()
                .Where(u => u.Id == userId)
                .Select(u => new { u.UserType, u.ClientId })
                .FirstOrDefaultAsync(ct);

            if (user == null)
                return query.Where(_ => false);

            switch (user.UserType)
            {
                case TicketAudience.Admin:
                    return query;

                case TicketAudience.Support:
                    // Every support agent assigned to a client sees and answers all of that client's tickets.
                    // A subquery keeps it to one round trip (and still works with many assignments).
                    return query.Where(t =>
                        t.SupportedById == userId ||
                        (t.ClientId.HasValue && _context.UserClientAssignments
                            .Any(a => a.UserId == userId && a.ClientId == t.ClientId)));

                case TicketAudience.Client:
                    return user.ClientId.HasValue
                        ? query.Where(t => t.ClientId == user.ClientId)
                        : query.Where(_ => false);

                case TicketAudience.Developer:
                    return query.Where(t => t.ProjectId.HasValue &&
                        _context.UserProjects.Any(p => p.UserAccountId == userId && p.ProjectId == t.ProjectId));

                default:
                    return query.Where(t => t.RequestedById == userId ||
                                            (user.ClientId != null && t.ClientId == user.ClientId));
            }
        }

        #endregion

        #region Optional loads

        private async Task LoadMessagesAsync(List<TicketDto> tickets, List<int> ticketIds, CancellationToken ct)
        {
            var messages = await _context.TicketMessages
                .AsNoTracking()
                .Where(m => ticketIds.Contains(m.TicketId))
                .OrderBy(m => m.DateCreated)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    TicketId = m.TicketId,
                    Content = m.Content,
                    IsUser = m.IsUser,
                    IsCase = m.IsCase,
                    IsAutoReply = m.IsAutoReply,
                    SenderId = m.SenderId,
                    DateCreated = m.DateCreated,
                    SenderAvatar = "images/default.png",
                    Attachments = m.TicketUploads
                        .Where(u => !u.IsDeleted)
                        .Select(u => new TicketUploadDto
                        {
                            Id = u.Id,
                            FileName = u.FileName,
                            FileType = u.FileType,
                            Base64Content = string.Empty,
                            ThumbnailBase64 = u.ThumbnailBase64,
                            UploadedById = u.UploadedById
                        }).ToList()
                })
                .ToListAsync(ct);

            var lookup = messages.GroupBy(m => m.TicketId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var t in tickets)
                t.Messages = lookup.GetValueOrDefault(t.Id, new());
        }

        private async Task LoadHistoriesAsync(List<TicketDto> tickets, List<int> ticketIds, CancellationToken ct)
        {
            var histories = await _context.TicketHistories
                .AsNoTracking()
                .Where(h => ticketIds.Contains(h.TicketId))
                .OrderBy(h => h.UpdatedAt)
                .Select(h => new TicketHistoryDto
                {
                    Id = h.Id,
                    TicketId = h.TicketId,
                    OldStatus = h.OldStatus.ToString(),
                    NewStatus = h.NewStatus.ToString(),
                    UpdatedBy = h.UpdatedBy,
                    UpdatedAt = h.UpdatedAt,
                    Remarks = h.Remarks
                })
                .ToListAsync(ct);

            var lookup = histories.GroupBy(h => h.TicketId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var t in tickets)
                t.Histories = lookup.GetValueOrDefault(t.Id, new());
        }

        #endregion

        private static DateTime? Latest(params DateTime?[] values)
        {
            DateTime? max = null;
            foreach (var v in values)
                if (v.HasValue && (!max.HasValue || v.Value > max.Value))
                    max = v;
            return max;
        }

        private static string Humanize(TimeSpan ts) =>
            ts.TotalMinutes switch
            {
                < 1 => "Just now",
                < 60 => $"{(int)ts.TotalMinutes}m ago",
                < 1440 => $"{(int)ts.TotalHours}h ago",
                < 10080 => $"{(int)ts.TotalDays}d ago",
                _ => $"{(int)(ts.TotalDays / 7)}w ago"
            };
    }
}

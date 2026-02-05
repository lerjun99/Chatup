using AutoMapper;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class GetTicketsHandler : IRequestHandler<GetTicketsQueries, TicketPagedResponse>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepo;

        public GetTicketsHandler(IChatDBContext context, IMapper mapper, IUserRepository userRepo)
        {
            _context = context;
            _mapper = mapper;
            _userRepo = userRepo;
        }

        public async Task<TicketPagedResponse> Handle(GetTicketsQueries request, CancellationToken cancellationToken)
        {
            try
            {
                // 1️⃣ Base query
                var query = _context.Tickets.AsNoTracking();

                // 2️⃣ Apply status, search, priority, archived
                query = ApplyBasicFilters(query, request);

                // 3️⃣ Apply user access filter including ClientAssignments
                query = await ApplyUserFilterAsync(query, request.UserId, cancellationToken);

                // 4️⃣ Status counts for UI
                var statusCounts = await query
                    .GroupBy(t => t.Status)
                    .Select(g => new TicketStatusCountDto
                    {
                        Status = g.Key ?? TicketStatus.Open,
                        Count = g.Count()
                    })
                    .ToListAsync(cancellationToken);

                // 5️⃣ Total count for paging
                var totalCount = await query.CountAsync(cancellationToken);

                // 6️⃣ Paging and projection
                var tickets = await query
                    .OrderByDescending(t =>
                        _context.TicketMessages
                        .Where(m => m.TicketId == t.Id && m.IsUser)
                        .Select(m => m.DateCreated)
                        .Max() ?? t.DateReceived)
                    .ThenByDescending(t => t.Id)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(t => new TicketDto
                    {
                        Id = t.Id,
                        TicketNo = t.TicketNo,
                        CaseNo = t.CaseNo,
                        IsCase = t.IsCase,
                        IssueTitle = t.IssueTitle,
                        ProjectId = t.ProjectId,
                        Status = t.Status.ToString(),
                        DateReceived = t.DateReceived,
                        DueDate = t.DueDate,
                        Priority = t.Priority,
                        IsArchived = t.IsArchived,
                        RequestedById = t.RequestedById ?? 0,
                        RequestedByName = t.RequestedBy.FullName,
                        ClientId = t.ClientId ?? 0,
                        ClientName = t.Client.ClientName,
                        ClientAvatar = t.Client.PhotoUrl,
                        ClientEmail = t.Client.EmailAddress,
                        SupportedById = t.SupportedById ?? 0,
                        SupportedByName = t.SupportedBy.FullName,
                        Messages = new List<MessageDto>(),
                        Histories = new List<TicketHistoryDto>(),
                        ResponseTime = "No Messages Yet"
                    })
                    .ToListAsync(cancellationToken);

                if (!tickets.Any())
                    return new TicketPagedResponse
                    {
                        Tickets = new List<TicketDto>(),
                        StatusCounts = statusCounts
                    };

                var ticketIds = tickets.Select(t => t.Id).ToList();

                // 7️⃣ Include messages if requested
                if (request.IncludeMessages)
                    await LoadMessagesAsync(tickets, ticketIds, cancellationToken);

                // 8️⃣ Include ticket histories
                await LoadHistoriesAsync(tickets, ticketIds, cancellationToken);
                // 8️⃣ Response Time (ALWAYS)
                await LoadResponseTimesAsync(tickets, ticketIds, cancellationToken);

           

                return new TicketPagedResponse
                {
                    Tickets = tickets,
                    StatusCounts = statusCounts
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR in GetTicketsHandler: {ex}");
                return new TicketPagedResponse
                {
                    Tickets = new List<TicketDto>(),
                    StatusCounts = new List<TicketStatusCountDto>()
                };
            }
        }

        #region Helpers

        private IQueryable<ChatUp.Domain.Entities.Ticket> ApplyBasicFilters(
            IQueryable<ChatUp.Domain.Entities.Ticket> query, GetTicketsQueries request)
        {
            if (request.StatusFilter != TicketStatus.Closed)
                query = query.Where(t => t.Status != TicketStatus.Closed);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(t => t.IssueTitle.Contains(request.Search) || t.TicketNo.Contains(request.Search));

            if (request.StatusFilter.HasValue)
                query = query.Where(t => t.Status == request.StatusFilter);

            if (request.PriorityFilter.HasValue)
                query = query.Where(t => t.Priority == request.PriorityFilter);
            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(t =>
                    t.IssueTitle.Contains(request.Search) ||
                    t.TicketNo.Contains(request.Search));

            if (request.PriorityFilter.HasValue)
                query = query.Where(t => t.Priority == request.PriorityFilter);
            query = request.IncludeArchived
                ? query.Where(t => t.IsArchived)
                : query.Where(t => !t.IsArchived);

            return query;
        }

        private async Task<IQueryable<ChatUp.Domain.Entities.Ticket>> ApplyUserFilterAsync(
    IQueryable<ChatUp.Domain.Entities.Ticket> query,
    int userId,
    CancellationToken ct)
        {
            if (userId <= 0) return query.Where(_ => false); // no user -> no tickets

            // Get user info
            var user = await _userRepo.Query()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.UserType,
                    u.ClientId,
                    ProjectIds = u.UserProjects.Select(p => p.ProjectId)
                })
                .FirstOrDefaultAsync(ct);

            if (user == null) return query.Where(_ => false);

            switch (user.UserType)
            {
                case 1: // Admin
                    return query; // sees everything

                case 2: // Support-user: only tickets where assigned to client
                    var assignedClientIds = await _context.UserClientAssignments
                        .Where(a => a.UserId == user.Id)
                        .Select(a => a.ClientId)
                        .ToListAsync(ct);

                    if (!assignedClientIds.Any())
                    {
                        // No client assignments -> show nothing
                        return query.Where(_ => false);
                    }

                    // Only tickets assigned to the support user AND assigned client
                    return query.Where(t =>
                        t.ClientId.HasValue &&
                        assignedClientIds.Contains(t.ClientId.Value)
                    );

                case 3: // Client-level user
                    if (user.ClientId.HasValue)
                        return query.Where(t => t.ClientId == user.ClientId);
                    return query.Where(_ => false);

                case 4: // Project-level user
                    return query.Where(t => t.ProjectId.HasValue && user.ProjectIds.Contains(t.ProjectId.Value));

                default: // Other users: requested by or client
                    return query.Where(t => t.RequestedById == user.Id ||
                                            (user.ClientId != null && t.ClientId == user.ClientId));
            }
        }

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
                    SenderId = m.SenderId,
                    DateCreated = m.DateCreated,
                    SenderAvatar = m.Sender.Uploads.Select(u => u.Base64Content).FirstOrDefault() ?? "images/default.png",
                    Attachments = m.TicketUploads.Select(u => new TicketUploadDto
                    {
                        Id = u.Id,
                        FileName = u.FileName,
                        FileType = u.FileType,
                        Base64Content = u.Base64Content,
                        UploadedById = u.UploadedById

                    }).ToList()
                })
                .ToListAsync(ct);

            var messageLookup = messages.GroupBy(m => m.TicketId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var t in tickets)
                t.Messages = messageLookup.GetValueOrDefault(t.Id, new());
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

            var historyLookup = histories.GroupBy(h => h.TicketId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var t in tickets)
                t.Histories = historyLookup.GetValueOrDefault(t.Id, new());
        }
        private async Task LoadResponseTimesAsync(
           List<TicketDto> tickets,
           List<int> ticketIds,
           CancellationToken ct)
        {
            var lastMessages = await _context.TicketMessages
                .AsNoTracking()
                .Where(m => ticketIds.Contains(m.TicketId) &&
                            m.IsUser &&
                            m.DateCreated.HasValue)
                .GroupBy(m => m.TicketId)
                .Select(g => new
                {
                    TicketId = g.Key,
                    LastDate = g.Max(x => x.DateCreated),
                })
                .ToListAsync(ct);

            var lookup = lastMessages.ToDictionary(x => x.TicketId, x => x.LastDate);

            foreach (var t in tickets)
            {
                if (lookup.TryGetValue(t.Id, out var dt) && dt.HasValue)
                {
                    var span = DateTime.UtcNow - dt.Value;

                    t.ResponseTimeSeconds = (long)span.TotalSeconds; // ✅ sortable
                    t.ResponseTime = Humanize(span);                 // ✅ UI text
                }
                else
                {
                    // No response yet → highest priority
                    t.ResponseTimeSeconds = long.MaxValue;
                    t.ResponseTime = "No messages yet";
                }
            }
        }
        private static string Humanize(TimeSpan ts) =>
            ts.TotalMinutes switch
            {
                < 1 => "Just now",
                < 60 => $"{(int)ts.TotalMinutes}m ago",
                < 1440 => $"{(int)ts.TotalHours}h ago",
                _ => $"{(int)ts.TotalDays}d ago"
            };

        #endregion
    }
}
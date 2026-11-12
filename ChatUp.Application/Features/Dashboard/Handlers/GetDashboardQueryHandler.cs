using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Dashboard.DTOs;
using ChatUp.Application.Features.Dashboard.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Dashboard.Handlers
{
    public class GetDashboardQueryHandler
        : IRequestHandler<GetDashboardQuery, DashboardDto>
    {
        private readonly IChatDBContext _db;
        private readonly IUserRepository _userRepo;

        public GetDashboardQueryHandler(
            IChatDBContext db,
            IUserRepository userRepo)
        {
            _db = db;
            _userRepo = userRepo;
        }

        public async Task<DashboardDto> Handle(
            GetDashboardQuery request,
            CancellationToken cancellationToken)
        {
            // =============================
            // BASE QUERY (USER + DATE FILTER)
            // =============================
            IQueryable<ChatUp.Domain.Entities.Ticket> ticketsQuery = _db.Tickets.AsNoTracking().Where(a=>a.IsArchived == false);

            ticketsQuery = await ApplyUserFilterAsync(
                ticketsQuery,
                request.UserId,
                cancellationToken);

            if (request.From.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.DateReceived >= request.From.Value);

            if (request.To.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.DateReceived <= request.To.Value);
            // =============================
            // PROJECT FILTER
            // =============================
            if (request.ProjectId.HasValue && request.ProjectId > 0)
            {
                ticketsQuery = ticketsQuery
                    .Where(t => t.ProjectId == request.ProjectId);
            }
            // =============================
            // 1️⃣ TICKET COUNTS
            // =============================
            // Single round-trip instead of one COUNT query per status
            var counts = await ticketsQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Open = g.Sum(t => t.Status == TicketStatus.Open ? 1 : 0),
                    InProgress = g.Sum(t => t.Status == TicketStatus.InProgress ? 1 : 0),
                    Resolved = g.Sum(t => t.Status == TicketStatus.Resolved ? 1 : 0),
                    Closed = g.Sum(t => t.Status == TicketStatus.Closed ? 1 : 0),
                    Rejected = g.Sum(t => t.Status == TicketStatus.Rejected ? 1 : 0),
                    Critical = g.Sum(t => t.Priority == TicketPriority.Critical ? 1 : 0),
                    Breached = g.Sum(t => t.IsBreached ? 1 : 0)
                })
                .FirstOrDefaultAsync(cancellationToken);

            int total = counts?.Total ?? 0;
            int open = counts?.Open ?? 0;
            int inProgress = counts?.InProgress ?? 0;
            int resolved = counts?.Resolved ?? 0;
            int closed = counts?.Closed ?? 0;
            int rejected = counts?.Rejected ?? 0;
            int critical = counts?.Critical ?? 0;
            int breached = counts?.Breached ?? 0;

            double Pct(int v) => Math.Round(v * 100.0 / Math.Max(total, 1), 1);

            var ticketCountsDto = new TicketCountsDto(
                Open: open,
                InProgress: inProgress,
                Closed: closed,
                Rejected: rejected,
                Critical: critical,
                Total: total,
                OpenPercent: Pct(open),
                InProgressPercent: Pct(inProgress),
                ClosedPercent: Pct(closed),
                RejectedPercent: Pct(rejected),
                CriticalPercent: Pct(critical),
                OffsetCritical: 0,
                OffsetRejected: Pct(critical),
                OffsetClosed: Pct(critical) + Pct(rejected),
                OffsetInProgress: Pct(critical) + Pct(rejected) + Pct(closed),
                Breached: breached,
                Resolved: resolved
            );

            // =============================
            // 2️⃣ SLA ALERTS (PAGED)
            // =============================
            // Includes overdue tickets; finished tickets (closed/resolved/rejected) can't be at risk
            var now = DateTime.UtcNow;
            var nearSlaCutoff = now.AddHours(2);

            var slaBase = ticketsQuery
                .Where(t => t.DueDate != null &&
                            t.Status != TicketStatus.Closed &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Rejected);

            int page = Math.Max(1, request.SlaAlertsPage);
            int size = Math.Max(1, request.SlaAlertsPageSize);
            int totalSla = await slaBase.CountAsync(cancellationToken);
            int totalPages = (int)Math.Ceiling(totalSla / (double)size);

            var slaAlerts = await slaBase
                .OrderByDescending(t => t.IsBreached || t.DueDate <= now)
                .ThenBy(t => t.DueDate)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(t => new TicketSummaryDto
                {
                    Id = t.Id,
                    TicketNo = t.TicketNo,
                    ClientName = t.Client.ClientName ?? "",
                    Status = t.Status.ToString(),
                    DateReceived = t.DateReceived,
                    DueDate = t.DueDate,
                    IsBreached = t.IsBreached,
                    Subject = t.IssueTitle ?? "",
                    Severity = t.Priority.ToString(),
                    CreatedAt = t.DateReceived,
                    // Past due counts as breached even before the background job flags it
                    SlaStatus = t.IsBreached || t.DueDate <= now
                        ? "Breached"
                        : t.DueDate <= nearSlaCutoff
                            ? "NearSLA"
                            : "OnTrack"
                })
                .ToListAsync(cancellationToken);

            // =============================
            // 3️⃣ AGENT PRODUCTIVITY
            // =============================
            // Project only what is needed: the agent's first reply is computed in SQL
            // instead of loading every ticket with its full message history.
            var agentTickets = await ticketsQuery
                .Where(t => t.SupportedById != null)
                .Select(t => new
                {
                    t.SupportedById,
                    t.DateReceived,
                    t.ResolvedDate,
                    FirstReplyAt = t.Messages
                        .Where(m => m.SenderId == t.SupportedById)
                        .Min(m => m.DateCreated)
                })
                .ToListAsync(cancellationToken);

            var agentStats = agentTickets
     .GroupBy(t => t.SupportedById)
     .Select(g =>
     {
         var list = g.ToList();
         int count = list.Count;

         double FirstResponseMinutes(DateTime? firstReplyAt, DateTime received) =>
             firstReplyAt.HasValue ? (firstReplyAt.Value - received).TotalMinutes : 0.0;

         // -----------------------------
         // Avg First Response (minutes)
         // -----------------------------
         double avgFirstResponse = list
             .Select(t => FirstResponseMinutes(t.FirstReplyAt, t.DateReceived))
             .DefaultIfEmpty(0)
             .Average();

         // -----------------------------
         // Proactiveness %
         // -----------------------------
         double proactivePct = count == 0 ? 0 : list.Count(t =>
             t.FirstReplyAt.HasValue &&
             FirstResponseMinutes(t.FirstReplyAt, t.DateReceived) <= 15) * 100.0 / count;

         return new
         {
             AgentId = g.Key,
             TicketCount = count,
             AvgFirstResponse = avgFirstResponse,
             ProactivePct = proactivePct
         };
     })
     .ToList();

            var agentIds = agentStats
                .Where(a => a.AgentId != null)
                .Select(a => a.AgentId!.Value)
                .Distinct()
                .ToList();

            var users = await _db.UserAccounts
                .AsNoTracking()
                .Where(u => agentIds.Contains(u.Id ?? 0))
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync(cancellationToken);

            var agentDtoList = agentStats
                .Select(a =>
                {
                    var u = users.FirstOrDefault(x => x.Id == a.AgentId);
                    return new AgentStatDto(
                        Name: u?.FullName ?? "Unassigned",
                        TicketCount: a.TicketCount,
                        AvgFirstResponseMinutes: Math.Round(a.AvgFirstResponse, 1),
                        AvgIdleMinutes: 0,
                        ProactivenessPercent: Math.Round(a.ProactivePct, 0)
                    );
                })
                .OrderByDescending(a => a.TicketCount)
                .ToList();

            // =============================
            // 4️⃣ RECENT TICKETS
            // =============================
            var recentTickets = await ticketsQuery
                .OrderByDescending(t => t.DateReceived)
                .Take(10)
                .Select(t => new TicketSummaryDto
                {
                    Id = t.Id,
                    TicketNo = t.TicketNo,
                    ClientName = t.Client.ClientName ?? "",
                    Status = t.Status.ToString(),
                    DateReceived = t.DateReceived,
                    DueDate = t.DueDate,
                    IsBreached = t.IsBreached,
                    Subject = t.IssueTitle ?? "",
                    Severity = t.Priority.ToString(),
                    CreatedAt = t.DateReceived,
                    SlaStatus = t.IsBreached ? "Breached" : "OnTrack"
                })
                .ToListAsync(cancellationToken);

            // =============================
            // 5️⃣ AVERAGES
            // =============================
            var resolvedDurations = agentTickets
                .Where(t => t.ResolvedDate.HasValue)
                .Select(t => (t.ResolvedDate!.Value - t.DateReceived).TotalHours)
                .ToList();

            double avgResolution = resolvedDurations.Any() ? resolvedDurations.Average() : 0;

            // =============================
            // FINAL DTO
            // =============================
            return new DashboardDto(
                TicketCounts: ticketCountsDto,
                SlaAlerts: slaAlerts,
                AgentStats: agentDtoList,
                RecentTickets: recentTickets,
                FromPage: (page - 1) * size + 1,
                ToPage: Math.Min(page * size, totalSla),
                TotalRecords: totalSla,
                CurrentPage: page,
                TotalPages: totalPages,
                AvgFirstResponseMinutes: Math.Round(
                    agentStats.Any() ? agentStats.Average(a => a.AvgFirstResponse) : 0, 1),
                AvgResolutionHours: Math.Round(avgResolution, 1)
            );
        }

        // =============================
        // USER FILTER
        // =============================
        private async Task<IQueryable<ChatUp.Domain.Entities.Ticket>> ApplyUserFilterAsync(
            IQueryable<ChatUp.Domain.Entities.Ticket> query,
            int userId,
            CancellationToken ct)
        {
            if (userId <= 0)
                return query.Where(_ => false);

            var user = await _userRepo.Query()
                .Where(u => u.Id == userId )
                .Select(u => new
                {
                    u.Id,
                    u.UserType,
                    u.ClientId,
                    ProjectIds = u.UserProjects.Select(p => p.ProjectId)
                })
                .FirstOrDefaultAsync(ct);

            if (user == null)
                return query.Where(_ => false);

            return user.UserType switch
            {
                1 => query, // Admin

                2 => query.Where(t =>
                    t.ClientId.HasValue &&
                    _db.UserClientAssignments
                        .Where(a => a.UserId == user.Id)
                        .Select(a => a.ClientId)
                        .Contains(t.ClientId.Value)),

                3 => user.ClientId.HasValue
                        ? query.Where(t => t.ClientId == user.ClientId)
                        : query.Where(_ => false),

                4 => query.Where(t =>
                    t.ProjectId.HasValue &&
                    user.ProjectIds.Contains(t.ProjectId.Value)),

                _ => query.Where(t =>
                    t.RequestedById == user.Id ||
                    (user.ClientId != null && t.ClientId == user.ClientId))
            };
        }
    }
}
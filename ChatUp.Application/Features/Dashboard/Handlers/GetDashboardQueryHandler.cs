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
            // 1️⃣ TICKET COUNTS
            // =============================
            var total = await ticketsQuery.CountAsync(cancellationToken);
            total = total == 0 ? 1 : total;

            int Count(TicketStatus s) => ticketsQuery.Count(t => t.Status == s);

            int open = Count(TicketStatus.Open);
            int inProgress = Count(TicketStatus.InProgress);
            int closed = Count(TicketStatus.Closed);
            int rejected = Count(TicketStatus.Rejected);
            int critical = await ticketsQuery.CountAsync(
                t => t.Priority == TicketPriority.Critical,
                cancellationToken);

            double Pct(int v) => Math.Round(v * 100.0 / total, 1);

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
                OffsetInProgress: Pct(critical) + Pct(rejected) + Pct(closed)
            );

            // =============================
            // 2️⃣ SLA ALERTS (PAGED)
            // =============================
            var slaBase = ticketsQuery
                .Where(t => t.DueDate != null &&
                            t.DueDate > DateTime.UtcNow &&
                            t.Status != TicketStatus.Closed);

            int page = Math.Max(1, request.SlaAlertsPage);
            int size = Math.Max(1, request.SlaAlertsPageSize);
            int totalSla = await slaBase.CountAsync(cancellationToken);
            int totalPages = (int)Math.Ceiling(totalSla / (double)size);

            var slaAlerts = await slaBase
                .OrderByDescending(t => t.IsBreached)
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
                    SlaStatus = t.IsBreached
                        ? "Breached"
                        : (t.DueDate.HasValue &&
                           (t.DueDate.Value - DateTime.UtcNow).TotalHours <= 2 &&
                           (t.DueDate.Value - DateTime.UtcNow).TotalSeconds > 0)
                            ? "NearSLA"
                            : "OnTrack"
                })
                .ToListAsync(cancellationToken);

            // =============================
            // 3️⃣ AGENT PRODUCTIVITY
            // =============================
            var agentTickets = await ticketsQuery
                .Where(t => t.SupportedById != null)
                .Include(t => t.Messages)
                .ToListAsync(cancellationToken);

            var agentStats = agentTickets
     .GroupBy(t => t.SupportedById)
     .Select(g =>
     {
         var list = g.ToList();
         int count = list.Count;

         // -----------------------------
         // Avg First Response (minutes)
         // -----------------------------
         double avgFirstResponse = list
             .Select(t =>
             {
                 var msg = t.Messages
                     .Where(m => m.SenderId == t.SupportedById)
                     .OrderBy(m => m.DateCreated)
                     .FirstOrDefault();

                 if (msg?.DateCreated == null || t.DateReceived == null)
                     return 0.0;

                 return (msg.DateCreated.Value - t.DateReceived).TotalMinutes;
             })
             .DefaultIfEmpty(0)
             .Average();

         // -----------------------------
         // Proactiveness %
         // -----------------------------
         double proactivePct = count == 0 ? 0 : list.Count(t =>
         {
             var msg = t.Messages
                 .Where(m => m.SenderId == t.SupportedById)
                 .OrderBy(m => m.DateCreated)
                 .FirstOrDefault();

             if (msg?.DateCreated == null || t.DateReceived == null)
                 return false;

             return (msg.DateCreated.Value - t.DateReceived).TotalMinutes <= 15;
         }) * 100.0 / count;

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
            var resolved = agentTickets
                .Where(t => t.ResolvedDate.HasValue)
                .Select(t => (t.ResolvedDate.Value - t.DateReceived).TotalHours);

            double avgResolution = resolved.Any() ? resolved.Average() : 0;

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
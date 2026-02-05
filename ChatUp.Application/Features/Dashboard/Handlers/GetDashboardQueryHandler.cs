using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Dashboard.DTOs;
using ChatUp.Application.Features.Dashboard.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Dashboard.Handlers
{
    public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
    {
        private readonly IChatDBContext _db;

        public GetDashboardQueryHandler(IChatDBContext db)
        {
            _db = db;
        }

        public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            // ----------------------------
            // 1) Ticket Counts & Percentages
            // ----------------------------
            var ticketsQuery = _db.Tickets.AsNoTracking();

            if (request.From.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.DateReceived >= request.From.Value);
            if (request.To.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.DateReceived <= request.To.Value);

            var total = await ticketsQuery.CountAsync(cancellationToken);
            total = total == 0 ? 1 : total; // avoid divide by zero

            int GetCount(TicketStatus status) => ticketsQuery.Count(t => t.Status == status);

            int open = GetCount(TicketStatus.Open);
            int inProgress = GetCount(TicketStatus.InProgress);
            int resolved = GetCount(TicketStatus.Resolved);
            int closed = GetCount(TicketStatus.Closed);
            int rejected = GetCount(TicketStatus.Rejected);

            int critical = await ticketsQuery.CountAsync(t => t.Priority == TicketPriority.Critical, cancellationToken);

            double pctOrZero(int value) => (value * 100.0) / total;
            double criticalPct = Math.Round(pctOrZero(critical), 1);
            double rejectedPct = Math.Round(pctOrZero(rejected), 1);
            double closedPct = Math.Round(pctOrZero(closed), 1);
            var ticketCountsDto = new TicketCountsDto(
                Open: open,
                InProgress: inProgress,
                Closed: closed,
                Rejected: rejected,
                Critical: critical,
                Total: total,
                OpenPercent: Math.Round(pctOrZero(open), 1),
                InProgressPercent: Math.Round(pctOrZero(inProgress), 1),
                ClosedPercent: Math.Round(pctOrZero(closed), 1),
                RejectedPercent: Math.Round(pctOrZero(rejected), 1),
                CriticalPercent: Math.Round(pctOrZero(critical), 1),
                OffsetCritical: 0,
                OffsetRejected: Math.Round(criticalPct, 1),
                OffsetClosed: Math.Round(criticalPct + rejectedPct, 1),
                OffsetInProgress: Math.Round(criticalPct + rejectedPct + closedPct, 1)
            );

            // ----------------------------
            // 2) SLA Alerts (Paged) + SLA computation
            // ----------------------------
            var slaBase = _db.Tickets
                         .AsNoTracking()
                         .Where(t => t.DueDate != null && t.DueDate > DateTime.UtcNow && t.Status != TicketStatus.Closed);
            int currentPage = Math.Max(1, request.SlaAlertsPage);
            int pageSize = Math.Max(1, request.SlaAlertsPageSize);
            int totalSla = await slaBase.CountAsync(cancellationToken);
            int totalPages = (int)Math.Ceiling(totalSla / (double)pageSize);
            // Apply date filter
            if (request.From.HasValue)
                slaBase = slaBase.Where(t => t.DateReceived >= request.From.Value);
            if (request.To.HasValue)
                slaBase = slaBase.Where(t => t.DateReceived <= request.To.Value);

            var slaAlerts = await slaBase
                .OrderByDescending(t => t.IsBreached)
                .ThenBy(t => t.DueDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TicketSummaryDto
                {
                    Id = t.Id,
                    TicketNo = t.TicketNo,
                    ClientName = t.Client.ClientName ?? "",
                    Status = t.Status.ToString() ?? "",
                    DateReceived = t.DateReceived,
                    DueDate = t.DueDate,
                    IsBreached = t.IsBreached,
                    Subject = t.IssueTitle ?? "",
                    Severity = t.Priority.ToString(),
                    CreatedAt = t.DateReceived,
                    SlaStatus = t.IsBreached
                        ? "Breached"
                        : (t.DueDate.HasValue && (t.DueDate.Value - DateTime.UtcNow).TotalHours <= 2
                            && (t.DueDate.Value - DateTime.UtcNow).TotalSeconds > 0)
                            ? "NearSLA"
                            : "OnTrack"
                })
                .ToListAsync(cancellationToken);

            // ----------------------------
            // 3) Agent Productivity
            // ----------------------------
            var tickets = await _db.Tickets
                         .AsNoTracking()
                         .Where(t => t.SupportedById != null)
                         // Apply date filter
                         .Where(t => (!request.From.HasValue || t.DateReceived >= request.From.Value) &&
                                     (!request.To.HasValue || t.DateReceived <= request.To.Value))
                         .Include(t => t.Messages)
                         .ToListAsync(cancellationToken);   

            var agentStats = tickets
     .GroupBy(t => t.SupportedById)
     .Select(g =>
     {
         var ticketList = g.ToList();
         int ticketCount = ticketList.Count;

         // Fix for CS1061: Ensure that nullable TimeSpan is properly handled before accessing TotalMinutes
         double avgFirstResponse = ticketList
              .Select(t =>
              {
                  var firstMessage = t.Messages
                      .OrderBy(m => m.DateCreated)
                      .FirstOrDefault(m => m.SenderId == t.SupportedById);

                  TimeSpan? responseTime = firstMessage != null
                      ? firstMessage.DateCreated - t.DateReceived
                      : (TimeSpan?)null;

                  return responseTime.HasValue ? responseTime.Value.TotalMinutes : 0.0;
              })
              .DefaultIfEmpty(0) // Prevent exception if empty
              .Average();

         // Fix for CS1061: Ensure that nullable TimeSpan is properly handled before accessing TotalMinutes
         double avgIdle = ticketList.Average(t =>
         {
             var agentMessages = t.Messages
                 .Where(m => m.SenderId == t.SupportedById)
                 .OrderBy(m => m.DateCreated)
                 .ToList();

             double totalIdle = 0;
             for (int j = 1; j < agentMessages.Count; j++)
             {
                 TimeSpan? diff = agentMessages[j].DateCreated - agentMessages[j - 1].DateCreated;
                 totalIdle += diff.HasValue ? diff.Value.TotalMinutes : 0.0;
             }
             return totalIdle / (agentMessages.Count > 1 ? agentMessages.Count - 1 : 1);
         });

         // Fix for CS1061: Ensure that nullable TimeSpan is properly handled before accessing TotalMinutes
         double proactivePercent = ticketList.Count(t =>
         {
             var firstMessage = t.Messages
                 .OrderBy(m => m.DateCreated)
                 .FirstOrDefault(m => m.SenderId == t.SupportedById);

             if (firstMessage != null && firstMessage.DateCreated.HasValue && t.DateReceived != null)
             {
                 TimeSpan? responseTime = firstMessage.DateCreated.Value - t.DateReceived;
                 return responseTime.HasValue && responseTime.Value.TotalMinutes <= 15;
             }
             return false;
         }) * 100.0 / ticketCount;

         return new
         {
             AgentId = g.Key,
             TicketCount = ticketCount,
             AvgFirstResponseMinutes = avgFirstResponse,
             AvgIdleMinutes = avgIdle,
             ProactivenessPercent = proactivePercent
         };
     })
     .ToList();

            // Map agent IDs to names
            var agentIds = agentStats.Select(a => a.AgentId).Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
            var users = await _db.UserAccounts
                .AsNoTracking()
                .Where(u => agentIds.Contains(u.Id ?? 0))
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync(cancellationToken);

            var agentDtoList = agentStats.Select(a =>
            {
                var user = users.FirstOrDefault(u => u.Id == a.AgentId);
                return new AgentStatDto(
                    Name: user?.FullName ?? "Unassigned",
                    TicketCount: a.TicketCount,
                    AvgFirstResponseMinutes: Math.Round(a.AvgFirstResponseMinutes, 1),
                    AvgIdleMinutes: Math.Round(a.AvgIdleMinutes, 1),
                    ProactivenessPercent: Math.Round(a.ProactivenessPercent, 0)
                );
            }).OrderByDescending(x => x.TicketCount).ToList();

            // ----------------------------
            // 4) Recent Tickets + SLA
            // ----------------------------
            var recentTickets = await _db.Tickets
                 .AsNoTracking()
                 // Apply date filter
                 .Where(t => (!request.From.HasValue || t.DateReceived >= request.From.Value) &&
                             (!request.To.HasValue || t.DateReceived <= request.To.Value))
                 .OrderByDescending(t => t.DateReceived)        
                 .Take(10)
                .Select(t => new TicketSummaryDto
                {
                    Id = t.Id,
                    TicketNo = t.TicketNo,
                    ClientName = t.Client.ClientName ?? "",
                    Status = t.Status.ToString() ?? "",
                    DateReceived = t.DateReceived,
                    DueDate = t.DueDate,
                    IsBreached = t.IsBreached,
                    Subject = t.IssueTitle ?? "",
                    Severity = t.Priority.ToString(),
                    CreatedAt = t.DateReceived,
                    SlaStatus = t.IsBreached
                        ? "Breached"
                        : (t.DueDate.HasValue && (t.DueDate.Value - DateTime.UtcNow).TotalHours <= 2
                            && (t.DueDate.Value - DateTime.UtcNow).TotalSeconds > 0)
                            ? "NearSLA"
                            : "OnTrack"
                })
                .ToListAsync(cancellationToken);
            var ticketList = tickets.ToList();
            int ticketCount = ticketList.Count;

            // Fix for CS1061: Ensure that nullable TimeSpan is properly handled before accessing TotalMinutes
            double avgFirstResponse = ticketList
               .Select(t =>
               {
                   var firstMessage = t.Messages
                       .Where(m => m.SenderId == t.SupportedById)
                       .OrderBy(m => m.DateCreated)
                       .FirstOrDefault();

                   TimeSpan? responseTime = firstMessage != null
                       ? firstMessage.DateCreated - t.DateReceived
                       : (TimeSpan?)null;

                   return responseTime?.TotalMinutes; // Use ?. to safely get TotalMinutes
               })
               .Where(minutes => minutes.HasValue) // exclude tickets with no response
               .Select(minutes => minutes.Value)
               .DefaultIfEmpty(0) // in case there are no responses
               .Average();
            // Avg Resolution (hours)
            var resolutionTimes = ticketList
          .Where(t => t.ResolvedDate.HasValue) // only tickets that are resolved
          .Select(t => (t.ResolvedDate.Value - t.DateReceived).TotalHours);

            double avgResolution = resolutionTimes.Any()
                ? resolutionTimes.Average()
                : 0.0;
            // ----------------------------
            // Build Dashboard DTO
            // ----------------------------
            var dashboard = new DashboardDto(
                TicketCounts: ticketCountsDto,
                SlaAlerts: slaAlerts,
                AgentStats: agentDtoList,
                RecentTickets: recentTickets,
                FromPage: (currentPage - 1) * pageSize + 1,
                ToPage: Math.Min(currentPage * pageSize, totalSla),
                TotalRecords: totalSla,
                CurrentPage: currentPage,
                TotalPages: totalPages,
                AvgFirstResponseMinutes: Math.Round(avgFirstResponse, 1),
                AvgResolutionHours: Math.Round(avgResolution, 1)
            );

            return dashboard;
        }

    }
}

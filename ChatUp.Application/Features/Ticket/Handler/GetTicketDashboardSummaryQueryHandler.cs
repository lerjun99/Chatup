using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Application.Features.Ticket.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Handler
{
    public class GetTicketDashboardSummaryQueryHandler : IRequestHandler<GetTicketDashboardSummaryQuery, TicketDashboardDto>
    {
        private readonly IChatDBContext _context;

        public GetTicketDashboardSummaryQueryHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<TicketDashboardDto> Handle(GetTicketDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var tickets = _context.Tickets.AsNoTracking();

            var totalTickets = await tickets.CountAsync(cancellationToken);
            totalTickets = totalTickets == 0 ? 1 : totalTickets;

            var open = await tickets.CountAsync(x => x.Status == TicketStatus.Open, cancellationToken);
            var inProgress = await tickets.CountAsync(x => x.Status == TicketStatus.InProgress, cancellationToken);
            var resolved = await tickets.CountAsync(x => x.Status == TicketStatus.Resolved, cancellationToken);
            var closed = await tickets.CountAsync(x => x.Status == TicketStatus.Closed, cancellationToken);
            var rejected = await tickets.CountAsync(x => x.Status == TicketStatus.Rejected, cancellationToken);

            // SLA Compliance (Not Breached)
            var totalNonBreached = await tickets.CountAsync(x => !x.IsBreached, cancellationToken);
            var slaPercent = (double)totalNonBreached / totalTickets * 100;

            // Average First Response (ensure non-null)
            var firstResponseAvgRaw = await tickets
                .Where(x => x.FirstResponseAt != null)
                .AverageAsync(
                    x => (int?)EF.Functions.DateDiffMinute(x.DateReceived, x.FirstResponseAt),
                    cancellationToken
                );

            double avgFirstResponse = firstResponseAvgRaw ?? 0;

            // Average Resolution (ensure non-null)
            var resolutionAvgRaw = await tickets
                .Where(x => x.ResolvedDate != null)
                .AverageAsync(
                    x => (int?)EF.Functions.DateDiffHour(x.DateReceived, x.ResolvedDate),
                    cancellationToken
                );

            double avgResolution = resolutionAvgRaw ?? 0;

            return new TicketDashboardDto
            {
                Open = open,
                InProgress = inProgress,
                Resolved = resolved,
                Closed = closed,
                Rejected = rejected,

                Low = await tickets.CountAsync(x => x.Priority == TicketPriority.Low, cancellationToken),
                Medium = await tickets.CountAsync(x => x.Priority == TicketPriority.Medium, cancellationToken),
                High = await tickets.CountAsync(x => x.Priority == TicketPriority.High, cancellationToken),
                Critical = await tickets.CountAsync(x => x.Priority == TicketPriority.Critical, cancellationToken),

                OpenPercent = (double)open / totalTickets * 100,
                SLACompliancePercent = Math.Round(slaPercent, 1),
                AvgFirstResponseMinutes = Math.Round(avgFirstResponse, 1),
                AvgResolutionHours = Math.Round(avgResolution, 1)
            };
        }
    }
}

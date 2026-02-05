using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Dashboard.DTOs
{
    public record TicketCountsDto(
       int Open,
       int InProgress,
       int Closed,
       int Rejected,
       int Critical,
       int Total,
       double OpenPercent,
       double InProgressPercent,
       double ClosedPercent,
       double RejectedPercent,
       double CriticalPercent,
       // donut offsets (cumulative for drawing stacked donut)
       double OffsetCritical,
       double OffsetRejected,
       double OffsetClosed,
       double OffsetInProgress
   );

    public record class TicketSummaryDto
    {
        public int Id { get; init; }
        public string TicketNo { get; init; }
        public string ClientName { get; init; }
        public string Status { get; init; }
        public DateTime DateReceived { get; init; }
        public DateTime? DueDate { get; init; }
        public bool IsBreached { get; init; }
        public string Subject { get; init; }
        public string Severity { get; init; }
        public DateTime CreatedAt { get; init; }
        public string SlaStatus { get; set; } = "OnTrack";
    }

    public record AgentStatDto(
        string Name,
        int TicketCount,
        double AvgFirstResponseMinutes,
        double AvgIdleMinutes,
        double ProactivenessPercent
    );

    public record DashboardDto(
        TicketCountsDto TicketCounts,
        IEnumerable<TicketSummaryDto> SlaAlerts,
        IEnumerable<AgentStatDto> AgentStats,
        IEnumerable<TicketSummaryDto> RecentTickets,
        // pagination meta for SLA Alerts
        int FromPage,
        int ToPage,
        int TotalRecords,
        int CurrentPage,
        int TotalPages,
    // NEW KPI metrics
    double AvgFirstResponseMinutes,
    double AvgResolutionHours
    );
}

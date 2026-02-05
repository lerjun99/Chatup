using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.DTOs
{
    public class TicketDashboardDto
    {
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
        public int Rejected { get; set; }

        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }
        public int Critical { get; set; }

        public double OpenPercent { get; set; }
        public double SLACompliancePercent { get; set; }
        public double AvgFirstResponseMinutes { get; set; }
        public double AvgResolutionHours { get; set; }
    }
}

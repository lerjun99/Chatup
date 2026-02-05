using ChatUp.Application.Features.Dashboard.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Dashboard.Queries
{
    public class GetDashboardQuery : IRequest<DashboardDto>
    {
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public int SlaAlertsPage { get; init; } = 1;
        public int SlaAlertsPageSize { get; init; } = 10;
    }
}

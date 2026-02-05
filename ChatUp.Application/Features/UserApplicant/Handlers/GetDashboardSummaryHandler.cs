using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.DTOs;
using ChatUp.Application.Features.UserApplicant.Queries;
using ChatUp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class GetDashboardSummaryHandler
: IRequestHandler<GetDashboardSummaryQuery, ApplicantDashboardDto>
    {
        private readonly IChatDBContext _context;


        public GetDashboardSummaryHandler(IChatDBContext context)
        {
            _context = context;
        }


        public async Task<ApplicantDashboardDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
        {
            var applicantsQuery = _context.Applicants.AsNoTracking().AsQueryable();

            // Apply date filter if provided
            if (request.FromDate.HasValue)
                applicantsQuery = applicantsQuery.Where(a => a.CreatedAt >= request.FromDate.Value.Date);
            if (request.ToDate.HasValue)
                applicantsQuery = applicantsQuery.Where(a => a.CreatedAt <= request.ToDate.Value.Date.AddDays(1).AddTicks(-1));

            // Total applicants
            var totalApplicants = await applicantsQuery.CountAsync(cancellationToken);

            // Count by status
            var statusGroups = await applicantsQuery
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            // Map to StatusCountDto and calculate percent
            var statusBreakdown = statusGroups
                .Select(s => new StatusCountDto
                {
                    Status = s.Status,
                    Count = s.Count,
                    Percent = totalApplicants > 0 ? (double)s.Count / totalApplicants * 100 : 0
                })
                .ToList();

            // Applicants by school
            var applicantsBySchool = await applicantsQuery
                 .GroupBy(a => a.School != null ? a.School.Trim() : "")
                 .Select(g => new SchoolCountDto
                 {
                     School = g.Key,
                     Total = g.Count()
                 })
                 .OrderByDescending(x => x.Total)
                 .ToListAsync(cancellationToken);

            // Recent applicants
            var recentApplicants = await applicantsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new RecentApplicantDto
                {
                    Id = a.Id ?? 0,
                    ApplicantName = a.ApplicantName,
                    School = a.School,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // Total counts
            int initialAssessmentCount = statusGroups.FirstOrDefault(s => s.Status == "Initial Assessment")?.Count ?? 0;
            int interviewCount = statusGroups.FirstOrDefault(s => s.Status == "For Interview")?.Count ?? 0;
            int assessmentCount = statusGroups.FirstOrDefault(s => s.Status == "Assessed")?.Count ?? 0;

            // ✅ New counts
            int shortlistedCount = statusGroups.FirstOrDefault(s => s.Status == "Shortlisted")?.Count ?? 0;
            int rejectedCount = statusGroups.FirstOrDefault(s => s.Status == "Rejected")?.Count ?? 0;
            int onboardingCount = statusGroups.FirstOrDefault(s => s.Status == "Onboarding")?.Count ?? 0;
            int backoutCount = statusGroups.FirstOrDefault(s => s.Status == "Backout")?.Count ?? 0;


            return new ApplicantDashboardDto
            {
                TotalApplicants = totalApplicants,
                InitialAssessmentCount = initialAssessmentCount,
                InterviewCount = interviewCount,
                AssessmentCount = assessmentCount,
                StatusBreakdown = statusBreakdown,
                ApplicantsBySchool = applicantsBySchool,
                RecentApplicants = recentApplicants,
                OnboardingCount = onboardingCount,
                BackoutCount = backoutCount,
                // Optional: add new KPI counts if you extend DTO
                ShortlistedCount = shortlistedCount,
                RejectedCount = rejectedCount
            };
        }
    }
}

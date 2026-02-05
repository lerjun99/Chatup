using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.DTOs
{
    public class ApplicantDashboardDto
    {
        public int TotalApplicants { get; set; }
        public int InitialAssessmentCount { get; set; }
        public int InterviewCount { get; set; }
        public int AssessmentCount { get; set; }
        public int ShortlistedCount { get; set; }    // ✅ Added
        public int RejectedCount { get; set; }       // ✅ Added
        public int OnboardingCount { get; set; }     // ✅ New
        public int BackoutCount { get; set; }        // ✅ New

        public List<StatusCountDto> StatusBreakdown { get; set; } = new();
        public List<SchoolCountDto> ApplicantsBySchool { get; set; } = new();
        public List<RecentApplicantDto> RecentApplicants { get; set; } = new();
    }

    public class SchoolCountDto
    {
        public string School { get; set; } = string.Empty;
        public int Total { get; set; }
    }
    public class RecentApplicantDto
    {
        public int Id { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class StatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percent { get; set; }
    }
}

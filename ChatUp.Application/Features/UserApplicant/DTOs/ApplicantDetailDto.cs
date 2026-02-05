using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.DTOs
{
    public class ApplicantDetailDto
    {
        public int Id { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string CvLink { get; set; } = string.Empty;
        public int RequiredHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool Screening { get; set; }
        public bool Interview { get; set; }
        public bool AcceptanceLetter { get; set; }
        public bool Orientation { get; set; }
        public bool Onboarding { get; set; }
        public bool IssuedCertificate { get; set; }
        public bool SendingEmail { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.DTOs
{
    public class UpdateApplicantInfoDto
    {
        public string ApplicantName { get; set; }
        public string School { get; set; }
        public string Batch { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string HomeAddress { get; set; }
        public string CvLink { get; set; }
        public int? RequiredHours { get; set; } // Make nullable to avoid default 0 issue
        public string? Status { get; set; }     // Optional
        public string? Remarks { get; set; }    // Optional
    }
}

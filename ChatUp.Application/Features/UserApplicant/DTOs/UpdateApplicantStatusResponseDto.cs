using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.DTOs
{
    public class UpdateApplicantStatusResponseDto
    {
        public int Id { get; set; }
        public string ApplicantName { get; set; }
        public string Email { get; set; }

        public string Status { get; set; }
        public bool Screening { get; set; }
        public bool Interview { get; set; }
        public bool AcceptanceLetter { get; set; }
        public bool Orientation { get; set; }
        public bool Onboarding { get; set; }

        public string Remarks { get; set; }
        public string Message { get; set; }
    }
}

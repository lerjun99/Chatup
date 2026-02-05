using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.DTOs
{
    public class UpdateApplicantStatusDto
    {
        public string Status { get; set; }

        public bool Screening { get; set; }
        public bool Interview { get; set; }
        public bool AcceptanceLetter { get; set; }
        public bool Orientation { get; set; }
        public bool Onboarding { get; set; }

        public string Remarks { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class Applicant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        public string ApplicantName { get; set; }
        public string Batch { get; set; }
        public string School { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string HomeAddress { get; set; }

        public DateTime ApplicationDate { get; set; }
        public string CvLink { get; set; }

        public string Status { get; set; } // Applied, Screened, Interviewed, Accepted, Rejected

        public bool Screening { get; set; }
        public bool Interview { get; set; }
        public bool AcceptanceLetter { get; set; }
        public bool Orientation { get; set; }
        public bool Onboarding { get; set; }

        public int RequiredHours { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string CertificateLink { get; set; }
        public bool IssuedCertificate { get; set; }
        public bool SendingEmail { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.DTOs
{
    public class ApplicantListDto
    {
        public int Id { get; set; }
        public string ApplicantName { get; set; }
        public string School { get; set; }
        public string Batch { get; set; }
        public string Status { get; set; }
        public DateTime ApplicationDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.DTOs
{
    public class UpdateUserProjectRequest
    {
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public int? UserType { get; set; }
    }
}

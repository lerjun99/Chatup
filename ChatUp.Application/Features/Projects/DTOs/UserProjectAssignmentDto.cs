using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.DTOs
{
    public class UserProjectAssignmentDto
    {
        public int Id { get; set; } // unique key
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int? UserType { get; set; }
    }

    public class CreateUserProjectAssignmentDto
    {
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int? UserType { get; set; }
    }
}

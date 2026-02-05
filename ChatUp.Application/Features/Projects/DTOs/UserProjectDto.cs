using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.DTOs
{
    public class UserProjectDto
    {
        public int UserAccountId { get; set; }
        public int ProjectId { get; set; }
        public int? UserType { get; set; }
        public string TeamName { get; set; }
        public string UserFullName { get; set; }
        public string ProjectTitle { get; set; }
    }

    public class CreateUserProjectDto
    {
        public int UserAccountId { get; set; }
        public int ProjectId { get; set; }
        public int? UserType { get; set; }
    }

    public class UpdateUserProjectDto
    {
        public int? UserType { get; set; }
    }
}

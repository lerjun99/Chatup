using ChatUp.Application.Features.UserRegistration.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }   // readable name from navigation
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<UserAccountDto> Users { get; set; } = new List<UserAccountDto>();
    }
    public class UserAccountDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public int? UserType { get; set; }
        public string TeamName { get; set; } // <-- team aligned here
        public string AvatarUrl { get; set; } = "images/default.png";

    }

    public class CreateProjectDto
    {
        public int ClientId { get; set; }
        public int TeamId { get; set; }
        public string Title { get; set; }
        public int CreatedBy { get; set; }
        public string Description { get; set; }
    }
    public class DeleteProjectDto
    {
        public int Id { get; set; }
        public bool DeletedBy { get; set; }
    }

    public class UpdateProjectDto
    {
        public int ClientId { get; set; }
        public int TeamId { get; set; }
        public int UpdatedBy { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}

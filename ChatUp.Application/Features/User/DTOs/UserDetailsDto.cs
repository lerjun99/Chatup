using ChatUp.Application.Features.UserRegistration.DTOs;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.DTOs
{
    public class UserDetailsDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public int? UserType { get; set; }
        public int? Status { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime? CreatedDate { get; set; }

        // Uploaded files
        public List<UploadedFileDto> UploadedFiles { get; set; } = new();

        // Add these to fix your error:
        public string? PhotoFileName => UploadedFiles.FirstOrDefault(f => f.FileType == "ProfileImg")?.Name;
        public string? PhotoPath => UploadedFiles.FirstOrDefault(f => f.FileType == "ProfileImg")?.Base64Content;


        // Client info
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public bool IsClient { get; set; }
        // ✅ Added: List of Client Assignments
        public List<UserClientAssignmentDto> ClientAssignments { get; set; } = new();
        // Contracts
        public List<UserContractDto> Contracts { get; set; } = new();

        // Projects
        public List<UserProjectDto> Projects { get; set; } = new();
    }

    public class UserContractDto
    {
        public int Id { get; set; }
        public string ContractName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UserProjectDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}

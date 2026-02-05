using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.DTOs
{
    public class UserClientAssignmentDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ClientId { get; set; }
        public int? UserType { get; set; }
        public string? Location { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? UserEmail { get; set; }
        public string? AvatarUrl { get; set; }
        public string? ClientName { get; set; }

        // ✅ Sub-list of users with same client
        public List<UserAccountSimpleDto> UserAccounts { get; set; } = new();
    }

    public class UserAccountSimpleDto
    {
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? EmailAddress { get; set; }
        public int? isLoggedIn { get; set; }
    }
}

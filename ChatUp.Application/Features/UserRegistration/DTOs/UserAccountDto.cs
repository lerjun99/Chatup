using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.DTOs
{
    public class UserAccountDto
    {
        public int? Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
        public int? Role { get; set; }
        public bool IsClient { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public int? UserType { get; set; }
        public int? IsActive { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string AvatarUrl { get; set; } = "images/default.png";

        // ✅ New properties for login tracking
        public DateTime? LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public double? DurationMinutes { get; set; }
        public DateTime? LastLogin { get; set; }
        public string LastActiveStatus { get; set; } = "Offline"; // ✅ add this
        public int UnreadCount { get; set; }
        public int? UserType { get; set; }
        public int ClientId { get; set; }
        public bool IsOnline { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.DTOs
{
    public class UserClientDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
        public int? ClientId { get; set; }
        public List<string> AvatarUrls { get; set; } = new();
    }
}

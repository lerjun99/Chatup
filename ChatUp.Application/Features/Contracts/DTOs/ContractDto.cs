using ChatUp.Application.Features.User.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.DTOs
{
    public class ContractDto
    {
        public int Id { get; init; }
        public string Title { get; init; }
        public string Description { get; init; }
        public int? UserType { get; init; }
        public string EmailAddress { get; set; } = "";


        public DateTime? StartDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public bool IsTerminated { get; init; }
        // ✅ Add client and related IDs
        public int ClientId { get; set; }
        public string? ClientName { get; set; }
        public List<string> ProjectTitles { get; set; } = new();
        public List<int> ProjectIds { get; init; } = new();
        public List<int> UserIds { get; init; } = new();

        // ✅ Add related client users
        public List<UserDto> ClientUsers { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Client.DTOs
{
    public class AssignedClientDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string PhotoUrl { get; set; }
        public string? EmailAddress { get; set; }
        public string? TelephoneNumber { get; set; }
    }
}

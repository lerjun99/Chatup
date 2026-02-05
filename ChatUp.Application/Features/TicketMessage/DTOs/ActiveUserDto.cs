using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class ActiveUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public bool IsUser { get; set; }
    }
}

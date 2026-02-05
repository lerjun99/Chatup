using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class TicketConversationDto
    {
        public List<MessageDto> Messages { get; set; } = new();
        public List<ActiveUserDto> ActiveUsers { get; set; } = new();
        // ✅ Pagination properties
        public int TotalMessages { get; set; }       // Total number of messages in this conversation
        public int PageNumber { get; set; }          // Current page number
        public int PageSize { get; set; }            // Number of messages per page
    }
}

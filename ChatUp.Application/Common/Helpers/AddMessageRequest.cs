using ChatUp.Application.Features.TicketMessage.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Helpers
{
    public class AddMessageRequest
    {
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public List<TicketUploadDto>? Attachments { get; set; }
    }
}

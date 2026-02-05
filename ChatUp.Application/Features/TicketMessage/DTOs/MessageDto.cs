using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public bool IsCase { get; set; }
        public DateTime? DateCreated { get; set; } // nullable to prevent SqlNullValueException
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public int TicketId { get; set; }  // <-- Add this
        public int CaseId { get; set; }  // <-- Add this
        public List<TicketUploadDto> Attachments { get; set; } = new();
    }
}

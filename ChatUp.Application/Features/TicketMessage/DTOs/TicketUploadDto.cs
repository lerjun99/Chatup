using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class TicketUploadDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? TicketMessageId { get; set; } // ✅ added nullable
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
        public string? ThumbnailBase64 { get; set; }
        public DateTime DateUploaded { get; set; }
        public int UploadedById { get; set; }
    }
}

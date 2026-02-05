using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class TicketUpload
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int? TicketMessageId { get; set; } // nullable
        public TicketMessage? TicketMessage { get; set; }

        public int UploadedById { get; set; }
        public UserAccount UploadedBy { get; set; }

        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Base64Content { get; set; }
        public string? ThumbnailBase64 { get; set; }
        public DateTime DateUploaded { get; set; }
        // ✅ Soft delete flag
        public bool IsDeleted { get; set; } = false;
    }
}

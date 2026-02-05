using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class TicketHistoryDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Remarks { get; set; }
    }
}

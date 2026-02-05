using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class TicketHistory
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Ticket))]
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public TicketStatus OldStatus { get; set; }
        public TicketStatus NewStatus { get; set; }
        // Priority change
        public TicketPriority? OldPriority { get; set; }
        public TicketPriority? NewPriority { get; set; }
        public int? UpdatedBy { get; set; } 

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? Remarks { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string TicketNo { get; private set; } = string.Empty;
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string CaseNo { get; private set; } = string.Empty;

        public DateTime DateReceived { get; set; } = DateTime.UtcNow;
        [MaxLength(250)] public string IssueTitle { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int? RequestedById { get; set; }
        public UserAccount? RequestedBy { get; set; }

        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? SupportedById { get; set; }
        public UserAccount? SupportedBy { get; set; }

        public TicketStatus? Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public bool IsBreached { get; set; }
        public bool IsCase { get; set; }
        public bool IsArchived { get; set; } = false;
        [Column(TypeName = "datetime")]
        public DateTime? DateUpdated { get; set; }

        public int? UpdatedBy { get; set; }
        // 🕓 Audit trail
        public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
        public ICollection<TicketUpload> TicketUploads { get; set; } = new List<TicketUpload>();

        public virtual List<TicketMessage> Messages { get; set; } = new();

        public DateTime? FirstResponseAt { get; set; }
    }

    public enum TicketStatus
    {
        Open = 1,
        InProgress = 2,
        Resolved = 3,
        Closed = 4,
        Rejected = 5,
        New = 6
    }

    public enum TicketPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}

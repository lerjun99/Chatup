using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class TicketMessage
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public int SenderId { get; set; }
        public UserAccount Sender { get; set; } = null!;

        public bool IsUser { get; set; }   // True = client/user, False = support agent
        public bool IsCase { get; set; }   // True = client/user, False = support agent
        public string Content { get; set; } = string.Empty;

        public DateTime? DateCreated { get; set; } = DateTime.UtcNow;

        public List<TicketInteraction> Interactions { get; set; } = new();
        public virtual ICollection<TicketUpload> TicketUploads { get; set; } = new List<TicketUpload>();
    }
}

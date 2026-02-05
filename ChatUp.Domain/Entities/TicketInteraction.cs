using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class TicketInteraction
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public DateTime LastMessageTime { get; set; }
        public TicketMessage? TicketMessage { get; set; }
    }
}

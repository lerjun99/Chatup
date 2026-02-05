using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class TicketRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        [Required]
        public int SupportUserId { get; set; }
        public UserAccount SupportUser { get; set; } = null!;

        [Required]
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime RatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.DTOs
{
    public class TicketHistoryDto
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        /// <summary>
        /// The previous status before update (e.g., Open)
        /// </summary>
        public string? OldStatus { get; set; }

        /// <summary>
        /// The new status after update (e.g., In Progress)
        /// </summary>
        public string? NewStatus { get; set; }

        /// <summary>
        /// Optional description or comment on the update
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// The ID of the user who made the update
        /// </summary>
        public int? UpdatedById { get; set; }

        /// <summary>
        /// The full name of the person who made the update
        /// </summary>
        public string? UpdatedByName { get; set; }

        /// <summary>
        /// When the update happened (UTC)
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}

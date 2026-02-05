using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Tickets.Commands.UpdateTicket
{
    public class UpdateTicketCommand : IRequest<Unit>
    {
        public int Id { get; set; }
        public string IssueTitle { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public int? RequestedById { get; set; }
        public int? ClientId { get; set; }
        public int? ProjectId { get; set; }
        public int? SupportedById { get; set; }
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }
        public int? UpdatedBy { get; set; } // ✅ New
        public string? Remarks { get; set; } // ✅ Optional
    }
}
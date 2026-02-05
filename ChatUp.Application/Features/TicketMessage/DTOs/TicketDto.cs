using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{

    public class TicketDto
    {
        public int Id { get; set; }
        public string TicketNo { get; set; } = string.Empty;
        public string CaseNo { get; set; } = string.Empty;
        public string IssueTitle { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DateReceived { get; set; }
        public DateTime? DueDate { get; set; }
        // Add these two to track sender/receiver
        public int RequestedById { get; set; }
        public int UploadedById { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientAvatar { get; set; } = string.Empty;
        public int SupportedById { get; set; }
        public string SupportedByName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public TicketPriority Priority { get; set; } // ⚠ must be enum, NOT string
        public bool IsArchived { get; set; }
        public bool IsCase { get; set; }
       
        public List<MessageDto> Messages { get; set; } = new();
        public string ResponseTime { get; set; } = string.Empty;
        // ✅ ADD THIS (used ONLY for sorting)
        public long ResponseTimeSeconds { get; set; }
        public Dictionary<int, string> ResponseTimesByUser { get; set; } = new();
        public List<TicketHistoryDto> Histories { get; set; } = new();
    }
}

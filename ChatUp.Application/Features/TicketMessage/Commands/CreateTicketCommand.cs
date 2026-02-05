using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Commands
{
    public class CreateTicketCommand : IRequest<TicketDto>
    {
        public string IssueTitle { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public int? ClientId { get; set; }
        public int? RequestedById { get; set; }
        public int SupportedById { get; set; }
        public DateTime DueDate { get; set; } = DateTime.UtcNow;
        public string InitialMessage { get; set; } = string.Empty;

        // Extra properties used in your modal
        public string Concern { get; set; } = string.Empty;
        public string Status { get; set; } = TicketStatus.Open.ToString();
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public bool IsCase { get; set; } = false;
        // ✅ Add this constructor
        public CreateTicketCommand(string issueTitle, int projectId, int? clientId, int requestedById, DateTime dueDate, string initialMessage)
        {
            IssueTitle = issueTitle;
            ProjectId = projectId;
            ClientId = clientId;
            RequestedById = requestedById;
            DueDate = dueDate;
            InitialMessage = initialMessage;
        }

        // Optional: parameterless constructor for model binding
        public CreateTicketCommand() { }


    }
}
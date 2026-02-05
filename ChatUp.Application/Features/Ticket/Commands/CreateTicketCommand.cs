using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Commands;

public class CreateTicketCommand : IRequest<TicketCreatedDto>
{
    public DateTime DateReceived { get; set; } = DateTime.UtcNow;
    public string IssueTitle { get; set; } = string.Empty;
    public string Concern { get; set; } = string.Empty;

    // ✅ Use IDs instead of strings
    public int ProjectId { get; set; }
    public int? RequestedById { get; set; }
    public int ClientId { get; set; }
    public int SupportedById { get; set; }
    public string Status { get; set; } = "Open";
    public string? ProjectName { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public bool IsCase { get; set; } = false;
}
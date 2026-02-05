using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.DTOs;

public class TicketCreatedDto
{
    public int Id { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string IssueTitle { get; set; } = string.Empty;
    public string Concern { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketPriority Priority { get; set; }
    // ✅ Add IsCase
    public bool IsCase { get; set; } = false;

    // Optional: If you want a separate CaseNo
    public string CaseNo { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }
    public string DeveloperEmail { get; set; } = "";
    public string DeveloperName { get; set; } = "";


}

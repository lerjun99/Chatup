using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.DTOs;


public record TicketDto(
    int Id,
    string TicketNo,
    DateTime DateReceived,
    string IssueTitle,
    string Concern,
    string Description,
    int? RequestedById,
    string RequestedByName,
    int? ClientId,
    string ClientName,
    int? ProjectId,
    string ProjectName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TicketStatus Status,
    int? SupportedById,
    string SupportedByName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TicketPriority Priority,
    DateTime? DueDate,
    bool IsBreached,
    bool IsArchived,
    string? RequesterEmail,        // requester's email
    string? ClientEmail = null,    // optional: client email if needed
    string? DeveloperEmail = null, // optional: developer email
    string? DeveloperName = null   // optional: developer name

);
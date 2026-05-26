using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GetTicketByIdHandler : IRequestHandler<GetTicketByIdQuery, TicketDto?>
{
    private readonly ITicketRepository _repo;
    private readonly IBusinessCalendarRepository _calendarService;
    public GetTicketByIdHandler(ITicketRepository repo, IBusinessCalendarRepository calendarService)
    {
        _calendarService = calendarService;
        _repo = repo;
    }

    public async Task<TicketDto?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var t = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null) return null;

        // ✅ Get business calendar
        var calendar = await _calendarService.GetByCountryAsync("PH");

        // Fallback if missing
        if (calendar == null)
            throw new Exception("Business calendar not configured");

        DateTime dueDate = t.DueDate ?? DateTime.UtcNow;

        bool isBreached = SlaHelper.CheckBreach(t);

        var remaining = dueDate - DateTime.UtcNow;

        string slaTime = SlaHelper.FormatRemainingTime(dueDate);

        string slaStatus = isBreached
            ? "Breached"
            : (remaining.TotalHours <= 2 ? "Near" : "OnTrack");

        return new TicketDto(
            t.Id,
            t.TicketNo ?? string.Empty,
            t.DateReceived,
            t.IssueTitle ?? string.Empty,
            t.Concern ?? string.Empty,
            t.Description ?? string.Empty,
            t.RequestedById,
            t.RequestedBy?.FullName ?? string.Empty,
            t.ClientId,
            t.Client?.ClientName ?? string.Empty,
            t.ProjectId,
            t.Project?.Title ?? string.Empty,
            t.Status ?? TicketStatus.Open,
            t.SupportedById,
            t.SupportedBy?.FullName ?? string.Empty,
            t.Priority,
            t.DueDate,

            // ✅ REPLACE STATIC BREACH
            isBreached,

            t.IsArchived,

            // emails
            t.RequestedBy?.EmailAddress ?? "",
            t.Client?.EmailAddress ?? "",
            t.SupportedBy?.EmailAddress ?? "",
            t.SupportedBy?.FullName ?? ""
        )
        {
            SlaTime = slaTime,
            SlaStatus = slaStatus
        };
    }
}
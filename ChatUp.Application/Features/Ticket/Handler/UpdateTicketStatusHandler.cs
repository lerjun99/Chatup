using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Handler
{
    public class UpdateTicketStatusHandler : IRequestHandler<UpdateTicketStatusCommand, bool>
    {
        private readonly IChatDBContext _db;
        private readonly IBusinessCalendarRepository _calendarService;
        private readonly IChatHubContext _chatHub;
        public UpdateTicketStatusHandler(IChatDBContext db, IChatHubContext chatHub, IBusinessCalendarRepository calendarService)
        {
            _db = db;
            _calendarService = calendarService;
            _chatHub = chatHub;
        }

        public async Task<bool> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
        {
            {
                try
                {
                    var ticket = await _db.Tickets
                        .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

                    if (ticket == null)
                        return false;

                    // =============================
                    // Capture OLD values
                    // =============================
                    var oldStatus = ticket.Status;
                    var oldPriority = ticket.Priority;
                    var oldProjectId = ticket.ProjectId;

                    // =============================
                    // Load calendar (safe, single call)
                    // =============================
                    var calendar = await _calendarService.GetByCountryAsync("PH");

                    if (calendar == null)
                        throw new Exception("Business calendar not found for PH");

                    // =============================
                    // APPLY UPDATES (SEQUENTIAL ONLY)
                    // =============================
                    if (request.NewStatus.HasValue)
                    {
                        ticket.Status = request.NewStatus.Value;

                        if (request.NewStatus == TicketStatus.Resolved ||
                            request.NewStatus == TicketStatus.Closed)
                        {
                            ticket.ResolvedDate = DateTime.UtcNow;
                        }
                    }

                    if (request.NewPriority.HasValue)
                    {
                        ticket.Priority = request.NewPriority.Value;
                    }

                    if (request.ProjectId.HasValue)
                    {
                        ticket.ProjectId = request.ProjectId.Value;
                    }

                    ticket.UpdatedBy = request.UpdatedBy;
                    ticket.SupportedById = request.UpdatedBy;
                    ticket.Concern = request.Remarks;

                    // =============================
                    // SLA UPDATE (SAFE)
                    // =============================
                    if (request.NewPriority.HasValue && oldPriority != request.NewPriority.Value)
                    {
                        ticket.DueDate = SlaHelper.CalculateDueDate(
                            request.NewPriority.Value,
                            calendar, ticket.DateReceived
                        );
                    }

                    // =============================
                    // TRACK CHANGES
                    // =============================
                    bool statusChanged = oldStatus != ticket.Status;
                    bool priorityChanged = oldPriority != ticket.Priority;
                    bool projectChanged = oldProjectId != ticket.ProjectId;

                    bool hasChanges = statusChanged || priorityChanged || projectChanged;

                    // =============================
                    // HISTORY LOG
                    // =============================
                    if (hasChanges)
                    {
                        _db.TicketHistories.Add(new TicketHistory
                        {
                            TicketId = ticket.Id,

                            OldStatus = oldStatus ?? TicketStatus.Open,
                            NewStatus = ticket.Status ?? TicketStatus.Open,

                            OldPriority = oldPriority,
                            NewPriority = ticket.Priority,

                            OldProjectId = oldProjectId,
                            NewProjectId = ticket.ProjectId,

                            UpdatedBy = request.UpdatedBy,
                            UpdatedAt = request.UpdatedAt,
                            Remarks = request.Remarks ?? "Ticket updated"
                        });
                    }

                    // =============================
                    // ACTIVITY LOGS
                    // =============================
                    if (statusChanged)
                    {
                        _db.ActivityLogs.Add(new ActivityLog
                        {
                            TicketId = ticket.Id,
                            ActorUserId = request.UpdatedBy,
                            ActivityType = ActivityType.TicketStatusChanged,
                            Summary = $"Status changed: {oldStatus} → {ticket.Status}",
                            OccurredAtUtc = request.UpdatedAt
                        });
                    }

                    if (priorityChanged)
                    {
                        _db.ActivityLogs.Add(new ActivityLog
                        {
                            TicketId = ticket.Id,
                            ActorUserId = request.UpdatedBy,
                            ActivityType = ActivityType.TicketPriorityChanged,
                            Summary = $"Priority changed: {oldPriority} → {ticket.Priority}",
                            OccurredAtUtc = request.UpdatedAt
                        });
                    }

                    if (projectChanged)
                    {
                        var projectIds = new List<int>();

                        if (oldProjectId.HasValue)
                            projectIds.Add(oldProjectId.Value);

                        if (ticket.ProjectId.HasValue)
                            projectIds.Add(ticket.ProjectId.Value);

                        var projectNames = await _db.Projects
                            .Where(p => projectIds.Contains(p.Id))
                            .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);

                        string oldProjectName = oldProjectId.HasValue && projectNames.ContainsKey(oldProjectId.Value)
                            ? projectNames[oldProjectId.Value]
                            : "N/A";

                        string newProjectName = ticket.ProjectId.HasValue && projectNames.ContainsKey(ticket.ProjectId.Value)
                            ? projectNames[ticket.ProjectId.Value]
                            : "N/A";

                        _db.ActivityLogs.Add(new ActivityLog
                        {
                            TicketId = ticket.Id,
                            ActorUserId = request.UpdatedBy,
                            ActivityType = ActivityType.TicketPriorityChanged, // ✅ FIXED (was wrong enum before)
                            Summary = $"Project changed: {oldProjectName} → {newProjectName}",
                            OccurredAtUtc = request.UpdatedAt
                        });
                    }

                    // =============================
                    // SAVE CHANGES (SINGLE CALL ONLY)
                    // =============================
                    await _db.SaveChangesAsync(cancellationToken);

                    // =============================
                    // SIGNALR NOTIFICATION (AFTER SAVE)
                    // =============================
                    await _chatHub.NotifyTicketUpdated();

                    return true;
                }
                catch (Exception ex)
                {
                    // Replace with ILogger in production
                    Console.Error.WriteLine($"UpdateTicketStatusHandler Error: {ex}");

                    return false;
                }
            }

        }
    }
}



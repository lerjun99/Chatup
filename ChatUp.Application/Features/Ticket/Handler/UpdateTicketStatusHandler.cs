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

        private readonly IChatHubContext _chatHub;
        public UpdateTicketStatusHandler(IChatDBContext db, IChatHubContext chatHub)
        {
            _db = db;

            _chatHub = chatHub;
        }

        public async Task<bool> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _db.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null)
                return false;

            var oldStatus = ticket.Status;
            var oldPriority = ticket.Priority;

            // Update fields
            if (request.NewStatus.HasValue)
            {
                ticket.Status = request.NewStatus.Value;
                if (request.NewStatus == TicketStatus.Resolved || request.NewStatus == TicketStatus.Closed)
                    ticket.ResolvedDate = DateTime.UtcNow;
            }

            if (request.NewPriority.HasValue)
                ticket.Priority = request.NewPriority.Value;

            ticket.UpdatedBy = request.UpdatedBy;
            ticket.SupportedById = request.UpdatedBy;
            ticket.Concern = request.Remarks;

            _db.Tickets.Update(ticket);

            // Add a single history record
            _db.TicketHistories.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                Ticket = ticket,
                OldStatus = oldStatus ?? TicketStatus.Open,
                NewStatus = ticket.Status ?? TicketStatus.Open,
                OldPriority = oldPriority,
                NewPriority = ticket.Priority,
                UpdatedBy = request.UpdatedBy,
                UpdatedAt = request.UpdatedAt,
                Remarks = request.Remarks ?? "Ticket updated"
            });

            await _db.SaveChangesAsync(cancellationToken);
            await _chatHub.NotifyTicketUpdated();

            return true;
        }
    }
}

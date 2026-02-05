using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ChatUp.Application.Tickets.Commands.UpdateTicket
{
    public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, Unit>
    {
        private readonly ITicketRepository _repo;
        public UpdateTicketCommandHandler(ITicketRepository repo, IChatHubContext chatHub)
        {
            _repo = repo;

        }

        public async Task<Unit> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {request.Id} not found.");

            var oldStatus = ticket.Status;

            ticket.IssueTitle = request.IssueTitle;
            ticket.Concern = request.Concern;
            ticket.RequestedById = request.RequestedById;
            ticket.ClientId = request.ClientId;
            ticket.ProjectId = request.ProjectId;
            ticket.SupportedById = request.UpdatedBy;
            ticket.Status = request.Status;
            ticket.Priority = request.Priority;

            // Update SLA if priority changed
            if (ticket.Priority != request.Priority)
            {
                ticket.Priority = request.Priority;
                ticket.DueDate = SlaHelper.CalculateDueDate(request.Priority);
            }

            // Save ticket changes
            await _repo.UpdateAsync(ticket, cancellationToken);

            // ✅ Record history
            if (oldStatus != request.Status)
            {
                var history = new TicketHistory
                {
                    TicketId = ticket.Id,
                    OldStatus = oldStatus ?? TicketStatus.Open,
                    NewStatus = request.Status,
                    UpdatedBy = request.UpdatedBy,
                    UpdatedAt = DateTime.UtcNow,
                    Remarks = request.Remarks
                };
                await _repo.AddHistoryAsync(history, cancellationToken);
            }
            return Unit.Value;
        }

    }
}
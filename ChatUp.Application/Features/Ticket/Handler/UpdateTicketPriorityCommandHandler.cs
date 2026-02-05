using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Handler
{
    public class UpdateTicketPriorityCommandHandler : IRequestHandler<UpdateTicketPriorityCommand, bool>
    {
        private readonly ITicketRepository _repo;

        public UpdateTicketPriorityCommandHandler(ITicketRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(UpdateTicketPriorityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ticket = await _repo.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket == null) return false;

            ticket.Priority = request.NewPriority;
            await _repo.UpdateAsync(ticket, cancellationToken);

            // Optional: Add a history record
            await _repo.AddHistoryAsync(new TicketHistory
            {
                TicketId = ticket.Id,
                Ticket = ticket, // attach the Ticket entity
                OldStatus = ticket.Status ?? TicketStatus.Open,
                NewStatus = ticket.Status ?? TicketStatus.Open,
                UpdatedBy = request.UpdatedBy,
                UpdatedAt = DateTime.UtcNow,
                Remarks = request.Remarks ?? $"Priority changed to {request.NewPriority}"
            }, cancellationToken);

            return true;
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error while creating ticket upload: {dbEx.Message}");
                if (dbEx.InnerException != null)
                    Console.WriteLine("INNER EXCEPTION: " + dbEx.InnerException.Message);

                throw;
            }
        }
    }
}

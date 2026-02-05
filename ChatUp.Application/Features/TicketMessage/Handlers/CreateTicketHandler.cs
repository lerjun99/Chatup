using AutoMapper;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class CreateTicketHandler : IRequestHandler<CreateTicketCommand, TicketDto>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;

        public CreateTicketHandler(IChatDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ticket = new Domain.Entities.Ticket
                {
                    IssueTitle = request.IssueTitle,
                    ProjectId = request.ProjectId,
                    ClientId = request.ClientId,
                    RequestedById = request.RequestedById,
                    SupportedById = request.SupportedById,
                    DueDate = request.DueDate,
                    DateReceived = DateTime.UtcNow,
                    Description = request.Concern,
                    ResolvedDate = null,
                    IsBreached = false,
                    Status = TicketStatus.Open, // optional default
                    Priority = request.Priority
                };

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.InitialMessage))
                {
                    var msg = new Domain.Entities.TicketMessage
                    {
                        TicketId = ticket.Id,
                        SenderId = request.RequestedById ?? 0,
                        IsUser = true,
                        Content = request.InitialMessage,
                        DateCreated = DateTime.UtcNow
                    };

                    _context.TicketMessages.Add(msg);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                var created = await _context.Tickets
                    .Include(t => t.Messages)
                    .FirstOrDefaultAsync(t => t.Id == ticket.Id, cancellationToken);

                return _mapper.Map<TicketDto>(created!);
            }
            catch (DbUpdateException dbEx)
            {
                // Log database-specific errors
                Console.WriteLine($"Database error while creating ticket: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log any other unexpected errors
                Console.WriteLine($"Unexpected error while creating ticket: {ex.Message}");
                throw;
            }
        }
    }
}

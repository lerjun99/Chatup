using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public record GetTicketsQuery(
    int UserId, // 👈 Added
    string? Search,
    TicketStatus? StatusFilter,
    TicketPriority? PriorityFilter,
    bool IncludeArchived,
    int Page,
    int PageSize
) : IRequest<PaginatedResponse<TicketDto>>;
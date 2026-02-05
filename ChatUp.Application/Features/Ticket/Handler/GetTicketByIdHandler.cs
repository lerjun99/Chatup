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
    public GetTicketByIdHandler(ITicketRepository repo) => _repo = repo;

    public async Task<TicketDto?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
       var t = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (t == null) return null;

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
          t.DueDate < DateTime.UtcNow || t.IsBreached,
          t.IsArchived,

          // ✔ Correct order
          t.RequestedBy?.EmailAddress ?? "",               // RequesterEmail  
          t.Client?.EmailAddress ?? "",                   // ClientEmail  
          t.SupportedBy?.EmailAddress ?? "",              // DeveloperEmail  
          t.SupportedBy?.FullName ?? ""                   // DeveloperName
      );
    }
}
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GetTicketsHandler : IRequestHandler<GetTicketsQuery, PaginatedResponse<TicketDto>>
{
    private readonly ITicketRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IChatDBContext _context;

    public GetTicketsHandler(ITicketRepository repo, IUserRepository userRepo, IChatDBContext context)
    {
        _repo = repo;
        _userRepo = userRepo;
        _context = context;
    }

    public async Task<PaginatedResponse<TicketDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query()
            .Where(t => t.Status != TicketStatus.New)
            .AsNoTracking();

        // 1️⃣ Hide closed tickets unless explicitly filtered
        if (!(request.StatusFilter.HasValue && request.StatusFilter.Value == TicketStatus.Closed))
            query = query.Where(t => t.Status != TicketStatus.Closed);

        // 2️⃣ Search filter
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(t => t.IssueTitle.Contains(request.Search) || t.TicketNo.Contains(request.Search));

        // 3️⃣ Status filter
        if (request.StatusFilter.HasValue)
            query = query.Where(t => t.Status == request.StatusFilter.Value);

        // 4️⃣ Priority filter
        if (request.PriorityFilter.HasValue)
            query = query.Where(t => t.Priority == request.PriorityFilter.Value);

        // 5️⃣ Archived filter
        query = request.IncludeArchived
            ? query.Where(t => t.IsArchived)
            : query.Where(t => !t.IsArchived);

        // 6️⃣ User-type filter
        if (request.UserId > 0)
        {
            var user = await _userRepo.Query()
                .Where(u => u.Id == request.UserId)
                .Select(u => new
                {
                    u.Id,
                    u.UserType,
                    u.ClientId,
                    ProjectIds = u.UserProjects.Select(p => p.ProjectId).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user != null)
            {
                switch (user.UserType)
                {
                    case 1:
                        // Admin — no restrictions
                        break;

                    case 2:
                        // Support-user — filter by assigned clients
                        var assignedClientIds = await _context.UserClientAssignments
                            .Where(a => a.UserId == user.Id)
                            .Select(a => a.ClientId)
                            .ToListAsync(cancellationToken);

                        if (!assignedClientIds.Any())
                        {
                            // No assignments -> return empty paginated result
                            return new PaginatedResponse<TicketDto>
                            {
                                Items = Array.Empty<TicketDto>(),
                                TotalCount = 0,
                                Page = request.Page,
                                PageSize = request.PageSize
                            };
                        }

                        query = query.Where(t =>
                            t.ClientId.HasValue && assignedClientIds.Contains(t.ClientId.Value));
                        break;

                    case 3:
                        // Client-level user
                        if (user.ClientId.HasValue)
                            query = query.Where(t => t.ClientId == user.ClientId);
                        else
                            query = query.Where(_ => false);
                        break;

                    case 4:
                        // Developer — filter by ProjectIds
                        if (user.ProjectIds.Any())
                            query = query.Where(t => t.ProjectId.HasValue && user.ProjectIds.Contains(t.ProjectId.Value));
                        else
                            query = query.Where(_ => false);
                        break;

                    default:
                        // Other users — tickets they requested or their client
                        query = query.Where(t =>
                            t.RequestedById == user.Id ||
                            (user.ClientId.HasValue && t.ClientId == user.ClientId));
                        break;
                }
            }
        }

        // 7️⃣ Count total after filters
        var totalCount = await query.CountAsync(cancellationToken);

        // 8️⃣ Pagination + eager loading
        var tickets = await query
            .OrderByDescending(t => t.DateReceived)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(t => t.RequestedBy)
            .Include(t => t.SupportedBy)
            .Include(t => t.Client)
            .Include(t => t.Project)
                .ThenInclude(p => p.UserProjects)
                    .ThenInclude(up => up.UserAccount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 9️⃣ Map to DTO
        var items = tickets.Select(t =>
        {
            var dev = t.Project?.UserProjects
                        .Select(up => up.UserAccount)
                        .FirstOrDefault(u => u.UserType == 4); // Developer

            return new TicketDto(
                t.Id,
                t.TicketNo,
                t.DateReceived,
                t.IssueTitle,
                t.Concern,
                t.Description,
                t.RequestedById,
                t.RequestedBy?.FullName ?? "",
                t.ClientId,
                t.Client?.ClientName ?? "",
                t.ProjectId,
                t.Project?.Title ?? "",
                t.Status ?? TicketStatus.Open,
                t.SupportedById,
                t.SupportedBy?.FullName ?? "",
                t.Priority,
                t.DueDate,
                t.DueDate < DateTime.UtcNow || t.IsBreached,
                t.IsArchived,
                t.Client?.EmailAddress,
                dev?.EmailAddress,
                dev?.FullName
            );
        }).ToList();

        return new PaginatedResponse<TicketDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
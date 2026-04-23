using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Activity.DTOs;
using ChatUp.Application.Features.Activity.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.Activity.Handlers
{
    public class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, ActivityPagedResponse>
    {
        private readonly IChatDBContext _db;
        private readonly IUserRepository _userRepo;

        public GetActivityLogsQueryHandler(IChatDBContext db, IUserRepository userRepo)
        {
            _db = db;
            _userRepo = userRepo;
        }

        public async Task<ActivityPagedResponse> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                return new ActivityPagedResponse();

            var user = await _userRepo.Query()
                .Where(u => u.Id == request.UserId)
                .Select(u => new
                {
                    u.Id,
                    u.UserType,
                    u.ClientId,
                    ProjectIds = u.UserProjects.Select(p => p.ProjectId)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                return new ActivityPagedResponse();

            var q = _db.ActivityLogs
                .AsNoTracking()
                .AsQueryable();

            if (request.From.HasValue)
                q = q.Where(x => x.OccurredAtUtc >= request.From.Value);

            if (request.To.HasValue)
                q = q.Where(x => x.OccurredAtUtc <= request.To.Value);

            // Apply same visibility rules as dashboard/tickets
            q = user.UserType switch
            {
                1 => q,

                2 => q.Where(a => a.TicketId != null &&
                    _db.Tickets
                        .Where(t => t.Id == a.TicketId)
                        .Where(t => t.ClientId.HasValue &&
                            _db.UserClientAssignments
                                .Where(uca => uca.UserId == user.Id)
                                .Select(uca => uca.ClientId)
                                .Contains(t.ClientId.Value))
                        .Any()),

                3 => user.ClientId.HasValue
                    ? q.Where(a => a.TicketId != null &&
                        _db.Tickets.Where(t => t.Id == a.TicketId && t.ClientId == user.ClientId).Any())
                    : q.Where(_ => false),

                4 => q.Where(a => a.TicketId != null &&
                        _db.Tickets.Where(t => t.Id == a.TicketId && t.ProjectId.HasValue && user.ProjectIds.Contains(t.ProjectId.Value)).Any()),

                _ => q.Where(a => a.TicketId != null &&
                        _db.Tickets.Where(t => t.Id == a.TicketId && (t.RequestedById == user.Id || (user.ClientId != null && t.ClientId == user.ClientId))).Any())
            };

            var total = await q.CountAsync(cancellationToken);

            var page = Math.Max(1, request.Page);
            var size = Math.Clamp(request.PageSize, 1, 200);
            var totalPages = (int)Math.Ceiling(total / (double)size);

            var items = await q
                .OrderByDescending(x => x.OccurredAtUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new ActivityLogDto
                {
                    Id = x.Id,
                    TicketId = x.TicketId,
                    TicketNo = x.TicketId != null ? _db.Tickets.Where(t => t.Id == x.TicketId).Select(t => t.TicketNo).FirstOrDefault() : null,
                    ActorUserId = x.ActorUserId,
                    ActorName = x.ActorUserId != null ? _db.UserAccounts.Where(u => u.Id == x.ActorUserId).Select(u => u.FullName).FirstOrDefault() ?? "System" : "System",
                    ActivityType = x.ActivityType.ToString(),
                    Summary = x.Summary,
                    OccurredAtUtc = x.OccurredAtUtc
                })
                .ToListAsync(cancellationToken);

            return new ActivityPagedResponse
            {
                Items = items,
                TotalRecords = total,
                CurrentPage = page,
                TotalPages = totalPages
            };
        }
    }
}

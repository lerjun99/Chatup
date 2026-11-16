using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.User.Handlers
{
    /// <summary>
    /// Every user↔client assignment with the client's own accounts. Projected in SQL: only the columns the page
    /// shows, and only the accounts of clients that actually have assignments (it used to load every user).
    /// </summary>
    public class GetUserClientAssignmentsByClientIdHandler
        : IRequestHandler<GetUserClientAssignmentsByClientIdQuery, List<UserClientAssignmentDto>>
    {
        private readonly IChatDBContext _context;

        public GetUserClientAssignmentsByClientIdHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<UserClientAssignmentDto>> Handle(GetUserClientAssignmentsByClientIdQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _context.UserClientAssignments
                .AsNoTracking()
                .Where(a => a.User != null && a.User.IsDeleted == 0)
                .Select(a => new UserClientAssignmentDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    ClientId = a.ClientId,
                    UserType = a.UserType,
                    UserName = a.User!.Username,
                    FullName = a.User.FullName,
                    UserEmail = a.Client != null ? a.Client.EmailAddress : null,
                    ClientName = a.Client != null ? a.Client.ClientName : string.Empty,
                    Location = a.Client != null ? a.Client.Location : null,
                    AvatarUrl = _context.UploadedFile
                        .Where(f => f.UserAccountId == a.UserId && f.FileType == "ProfileImg")
                        .OrderByDescending(f => f.Id)
                        .Select(f => f.Base64Content)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            if (assignments.Count == 0)
                return assignments;

            var clientIds = assignments
                .Where(a => a.ClientId.HasValue)
                .Select(a => a.ClientId!.Value)
                .Distinct()
                .ToList();

            var accountsByClient = (await _context.UserAccounts
                    .AsNoTracking()
                    .Where(u => u.ClientId.HasValue && clientIds.Contains(u.ClientId.Value) && u.IsDeleted == 0)
                    .Select(u => new
                    {
                        ClientId = u.ClientId!.Value,
                        Account = new UserAccountSimpleDto
                        {
                            Username = u.Username,
                            FullName = u.FullName,
                            EmailAddress = u.EmailAddress,
                            isLoggedIn = u.isLoggedIn
                        }
                    })
                    .ToListAsync(cancellationToken))
                .GroupBy(x => x.ClientId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Account).ToList());

            foreach (var a in assignments)
            {
                a.UserAccounts = a.ClientId.HasValue && accountsByClient.TryGetValue(a.ClientId.Value, out var accounts)
                    ? accounts
                    : new List<UserAccountSimpleDto>();
            }

            return assignments;
        }
    }
}

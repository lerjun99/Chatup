using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.Projects.Commands
{
    /// <summary>Adds several developers to one project at once; members already on it are skipped.</summary>
    public class AssignProjectMembersCommand : IRequest<AssignProjectMembersResult>
    {
        public int ProjectId { get; set; }
        public List<int> UserIds { get; set; } = new();
    }

    public class AssignProjectMembersResult
    {
        public int Added { get; set; }
        public int AlreadyAssigned { get; set; }
        public int Skipped { get; set; }
    }

    public class AssignProjectMembersHandler : IRequestHandler<AssignProjectMembersCommand, AssignProjectMembersResult>
    {
        private const int Developer = 4;
        private readonly IChatDBContext _context;

        public AssignProjectMembersHandler(IChatDBContext context) => _context = context;

        public async Task<AssignProjectMembersResult> Handle(AssignProjectMembersCommand request, CancellationToken ct)
        {
            var requested = (request.UserIds ?? new()).Where(id => id > 0).Distinct().ToList();

            if (request.ProjectId <= 0 || requested.Count == 0)
                throw new InvalidOperationException("Choose a project and at least one developer.");

            if (!await _context.Projects.AnyAsync(p => p.Id == request.ProjectId, ct))
                throw new InvalidOperationException("The selected project no longer exists.");

            var validIds = await _context.UserAccounts
                .Where(u => u.Id.HasValue && requested.Contains(u.Id.Value) && u.IsDeleted == 0 && u.UserType == Developer)
                .Select(u => u.Id!.Value)
                .ToListAsync(ct);

            var existing = await _context.UserProjects
                .Where(p => p.ProjectId == request.ProjectId && requested.Contains(p.UserAccountId))
                .Select(p => p.UserAccountId)
                .ToListAsync(ct);

            var toAdd = validIds.Except(existing).ToList();

            foreach (var userId in toAdd)
                _context.UserProjects.Add(new UserProject { UserAccountId = userId, ProjectId = request.ProjectId, UserType = Developer });

            if (toAdd.Count > 0)
                await _context.SaveChangesAsync(ct);

            return new AssignProjectMembersResult
            {
                Added = toAdd.Count,
                AlreadyAssigned = existing.Count,
                Skipped = requested.Count - validIds.Count
            };
        }
    }
}

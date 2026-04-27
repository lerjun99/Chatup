using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Application.Features.Projects.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Handlers
{
    public class GetProjectsByUserQueryHandler
      : IRequestHandler<GetProjectsByUserQuery, IEnumerable<ProjectDto>>
    {
        private readonly IProjectRepository _repo;
        private readonly IChatDBContext _context;

        public GetProjectsByUserQueryHandler(
            IProjectRepository repo,
            IChatDBContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IEnumerable<ProjectDto>> Handle(
            GetProjectsByUserQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Get user
            var user = await _context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            IQueryable<Project> query = _context.Projects
                .Include(p => p.Client)
                .Include(p => p.UserProjects)
                    .ThenInclude(up => up.UserAccount)
                        .ThenInclude(u => u.Uploads)
                .Where(p => !p.DeleteFlag).OrderBy(p=>p.Title);

            if (user.UserType == 3)
            {
                // Admin → all projects under same client
                query = query.Where(p => p.ClientId == user.ClientId);
            }
            else
            {
                // Non-admin → only assigned projects
                query = query.Where(p =>  !p.DeleteFlag);
            }

            var projects = await query.ToListAsync(cancellationToken);

            // 3. Map to DTO
            var result = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                ClientName = p.Client?.ClientName,
                Title = p.Title,
                Description = p.Description,

                Users = p.UserProjects?
                    .Select(up => new UserAccountDto
                    {
                        Id = up.Id,
                        FullName = up.UserAccount?.FullName,
                        UserId = up.UserAccountId,
                        UserType = up.UserType,
                        TeamName = up.TeamName,
                        AvatarUrl = up.UserAccount?.Uploads != null && up.UserAccount.Uploads.Any()
                            ? up.UserAccount.Uploads.First().Base64Content
                            : "images/default.png"
                    })
                    .ToList() ?? new List<UserAccountDto>()
            });

            return result;
        }
    }
}

using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Application.Features.Projects.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Handlers
{
    public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IEnumerable<ProjectDto>>
    {
        private readonly IProjectRepository _repo;

        public GetProjectsQueryHandler(IProjectRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
        {
            var projects = await _repo.GetAllAsync(cancellationToken);

            var result = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                ClientName = p.Client?.ClientName, // populate if Client navigation exists
                //TeamId = p.TeamId,
                //TeamName = p.Team?.TeamName,
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
                    : "images/default.png",
                    })
                    .ToList() ?? new List<UserAccountDto>()
            });

            return result;
        }
    }
}

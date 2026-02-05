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
    public class GetProjectsByClientQueryHandler : IRequestHandler<GetProjectsByClientQuery, IEnumerable<ProjectDto>>
    {
        private readonly IProjectRepository _repo;

        public GetProjectsByClientQueryHandler(IProjectRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<ProjectDto>> Handle(GetProjectsByClientQuery request, CancellationToken cancellationToken)
        {
            var projects = await _repo.GetByClientAsync(request.ClientId, cancellationToken);

            return projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                TeamId = p.TeamId,
                TeamName = p.Team?.TeamName,
                Title = p.Title,
                Description = p.Description
            }).ToList();
        }
    }
}

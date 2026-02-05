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
    public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
    {
        private readonly IProjectRepository _repo;

        public GetProjectByIdQueryHandler(IProjectRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            var p = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (p == null || p.DeleteFlag) return null;

            return new ProjectDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                TeamId = p.TeamId,
                TeamName = p.Team?.TeamName,
                Title = p.Title,
                Description = p.Description
            };
        }
    }
}

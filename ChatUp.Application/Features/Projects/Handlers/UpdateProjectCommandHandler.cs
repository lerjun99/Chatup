using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.Commands;
using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Handlers
{
    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
    {
        private readonly IProjectRepository _repo;
        private readonly IChatDBContext _db;

        public UpdateProjectCommandHandler(IProjectRepository repo, IChatDBContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            // 1. Get existing project
            var project = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (project == null || project.DeleteFlag)
                throw new KeyNotFoundException($"Project with id {request.Id} not found.");

            // 2. Validate TeamId
            var teamExists = await _db.Teams.AnyAsync(t => t.Id == request.UpdateDto.TeamId, cancellationToken);
            if (!teamExists)
                throw new ArgumentException($"Team with Id {request.UpdateDto.TeamId} does not exist.");

            // 3. Validate ClientId
            var clientExists = await _db.Client.AnyAsync(c => c.Id == request.UpdateDto.ClientId, cancellationToken);
            if (!clientExists)
                throw new ArgumentException($"Client with Id {request.UpdateDto.ClientId} does not exist.");

            // 4. Update fields
            project.ClientId = request.UpdateDto.ClientId;
            project.TeamId = request.UpdateDto.TeamId;
            project.Title = request.UpdateDto.Title;
            project.Description = request.UpdateDto.Description;
            project.UpdatedBy = request.UpdateDto.UpdatedBy;
            project.DateUpdated = DateTime.UtcNow;

            // 5. Save changes
            await _repo.UpdateAsync(project, cancellationToken);

            // 6. Reload updated project including navigations
            var updatedProject = await _db.Projects
                .Include(p => p.Client)
                .Include(p => p.Team)
                .Include(p => p.UserProjects)
                    .ThenInclude(up => up.UserAccount)
                .FirstOrDefaultAsync(p => p.Id == project.Id, cancellationToken);

            if (updatedProject == null)
                throw new Exception("Error loading updated project.");

            // 7. Map to DTO
            var projectDto = new ProjectDto
            {
                Id = updatedProject.Id,
                ClientId = updatedProject.ClientId,
                ClientName = updatedProject.Client?.ClientName,
                TeamId = updatedProject.TeamId,
                TeamName = updatedProject.Team?.TeamName,
                Title = updatedProject.Title,
                Description = updatedProject.Description,
                Users = updatedProject.UserProjects?
                    .Select(up => new UserAccountDto
                    {
                        Id = up.UserAccountId,
                        FullName = up.UserAccount?.FullName,
                        UserType = up.UserType,
                        TeamName = up.TeamName
                    })
                    .ToList() ?? new List<UserAccountDto>()
            };

            return projectDto;
        }
    }
}

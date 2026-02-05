using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.Commands;
using ChatUp.Application.Features.Projects.DTOs;
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
    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
    {
        private readonly IProjectRepository _repo;
        private readonly IChatDBContext _db; // <-- make sure you have a shared DbContext interface

        public CreateProjectCommandHandler(IProjectRepository repo, IChatDBContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
             var dto = request.CreateDto;

            // Validate Team
            var teamExists = await _db.Teams.AnyAsync(t => t.Id == dto.TeamId, cancellationToken);
            if (!teamExists)
                throw new ArgumentException($"Team with Id {dto.TeamId} does not exist.");

            // Validate Client
            var clientExists = await _db.Client.AnyAsync(c => c.Id == dto.ClientId, cancellationToken);
            if (!clientExists)
                throw new ArgumentException($"Client with Id {dto.ClientId} does not exist.");

            // Create Project entity
            var project = new Project
            {
                ClientId = dto.ClientId,
                TeamId = dto.TeamId,
                Title = dto.Title,
                Description = dto.Description,
                DateCreated = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy,
                DeleteFlag = false
            };

            // Save project
            await _repo.AddAsync(project, cancellationToken);

            // Reload project with navigation properties to get ClientName and TeamName
            var createdProject = await _db.Projects
                .Include(p => p.Client)
                .Include(p => p.Team)
                .Include(p => p.UserProjects)
                    .ThenInclude(up => up.UserAccount)
                .FirstOrDefaultAsync(p => p.Id == project.Id, cancellationToken);

            // Map to DTO
            var projectDto = new ProjectDto
            {
                Id = createdProject.Id,
                ClientId = createdProject.ClientId,
                ClientName = createdProject.Client?.ClientName,
                TeamId = createdProject.TeamId,
                TeamName = createdProject.Team?.TeamName,
                Title = createdProject.Title,
                Description = createdProject.Description,
                Users = createdProject.UserProjects?
                    .Select(up => new UserAccountDto
                    {
                        Id = up.UserAccountId,
                        FullName = up.UserAccount?.FullName,
                        UserType = up.UserType,
                        TeamName = up.TeamName
                    })
                    .ToList() ?? new System.Collections.Generic.List<UserAccountDto>()
            };

            return projectDto;
        }
    }
}

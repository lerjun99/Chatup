using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ChatDBContext _context;
        public ProjectRepository(ChatDBContext context) => _context = context;

        public async Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Projects
             .Include(p => p.Team)
              .Include(p => p.Client)
             .Include(p => p.UserProjects)
                 .ThenInclude(up => up.UserAccount) // <-- include the UserAccount here
                  .ThenInclude(u => u.Uploads) // <-- include uploads
             .Where(p => !p.DeleteFlag)
             .ToListAsync(cancellationToken);
        }

        public async Task<Project> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Projects
           .Include(p => p.Team)
            .Include(p => p.Client)
           .Include(p => p.UserProjects)
               .ThenInclude(up => up.UserAccount) // <-- include the UserAccount
                .ThenInclude(u => u.Uploads) // <-- include uploads
           .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
        public async Task<IEnumerable<Project>> GetByClientAsync(int clientId, CancellationToken cancellationToken = default)
        {
            return await _context.Projects
                .Include(p => p.Team)
                .Where(p => p.ClientId == clientId && !p.DeleteFlag)
                .ToListAsync(cancellationToken);
        }
        public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);
            return project;
        }

        public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
        {
            _context.Projects.Update(project);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SoftDeleteAsync(Project project, CancellationToken cancellationToken = default)
        {
            _context.Projects.Update(project);
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<UserAccount?> GetAssignedDeveloperAsync(int projectId, CancellationToken cancellationToken)
        {
            return await _context.UserProjects
                .Where(up => up.ProjectId == projectId)
                .Select(up => up.UserAccount)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}

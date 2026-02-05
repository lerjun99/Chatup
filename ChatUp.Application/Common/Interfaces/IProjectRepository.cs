using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Project> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Project>> GetByClientAsync(int clientId, CancellationToken cancellationToken = default);

        Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default);
        Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(Project project, CancellationToken cancellationToken = default);
        Task<UserAccount?> GetAssignedDeveloperAsync(int projectId, CancellationToken cancellationToken);
    }
}

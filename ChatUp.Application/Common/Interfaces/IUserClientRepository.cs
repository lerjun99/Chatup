using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IUserClientRepository
    {
        Task<List<UserClientAssignment>> GetAllAssignmentsAsync(CancellationToken cancellationToken);
        Task<List<UserClientAssignment>> GetAssignmentsByUserIdAsync(int userId, CancellationToken cancellationToken);
    }
}

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
    public class UserClientRepository : IUserClientRepository
    {
        private readonly ChatDBContext _context;

        public UserClientRepository(ChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<UserClientAssignment>> GetAllAssignmentsAsync(CancellationToken cancellationToken)
        {
            return await _context.UserClientAssignments
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserClientAssignment>> GetAssignmentsByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return await _context.UserClientAssignments
                .Where(a => a.UserId == userId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}

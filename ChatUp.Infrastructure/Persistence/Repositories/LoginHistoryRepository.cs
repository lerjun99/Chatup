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
    public class LoginHistoryRepository : ILoginHistoryRepository
    {
        private readonly ChatDBContext _context;

        public LoginHistoryRepository(ChatDBContext context)
        {
            _context = context;
        }

        public async Task AddAsync(LoginHistory history, CancellationToken cancellationToken = default)
        {
            _context.LoginHistories.Add(history);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<LoginHistory?> GetLastLoginAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.LoginHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LoginTime)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpdateAsync(LoginHistory history, CancellationToken cancellationToken = default)
        {
            _context.LoginHistories.Update(history);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

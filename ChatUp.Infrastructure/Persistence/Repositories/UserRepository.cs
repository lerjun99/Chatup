using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ChatDBContext _context;

        public UserRepository(ChatDBContext context)
        {
            _context = context;
        }
        public IQueryable<UserAccount> Query()
        {
            return _context.UserAccounts.AsQueryable();
        }
        public async Task<UserAccount?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
        {
             return await _context.UserAccounts
                .Include(u => u.Uploads).Where(a=>a.IsDeleted == 0)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }
        public async Task<List<UserAccount>> GetClientUsersAsync(int clientId)
        {
            return await _context.UserAccounts
                .Include(u => u.Uploads)
                .Where(u => u.IsClient && u.ClientId == clientId && u.IsDeleted != 1)
                .ToListAsync();
        }
        public async Task<List<UserAccount>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.UserAccounts
                .Include(u => u.Uploads).Where(a => a.IsDeleted == 0)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        public async Task<List<UserAccount>> GetUsersByClientAsync(int clientId, CancellationToken cancellationToken = default)
        {
            return await _context.UserClientAssignments
                .Include(uc => uc.User)
                    .ThenInclude(u => u.Uploads)
                .Where(uc => uc.ClientId == clientId && uc.User.IsDeleted == 0)
                .Select(uc => uc.User)
                .Distinct()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        public async Task<UserAccount?> GetUserWithDetailsAsync(int userId, CancellationToken cancellationToken)
        {
            return await _context.UserAccounts
                .Include(u => u.Uploads)
                .Include(u => u.Client)
                .Include(u => u.UserContracts)
                .Include(u => u.UserProjects)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && (u.IsDeleted ?? 0) == 0, cancellationToken);
        }

        public async Task<List<UserAccount>> GetAllUsersWithDetailsAsync(CancellationToken cancellationToken)
        {
            return await _context.UserAccounts
                .Include(u => u.Uploads)
                .Include(u => u.Client)
                .Include(u => u.UserContracts)
                .Include(u => u.UserProjects)
                .AsNoTracking()
                .Where(u => (u.IsDeleted ?? 0) == 0)
                .ToListAsync(cancellationToken);
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.UserAccounts
                .AsNoTracking()
                .AnyAsync(x => x.EmailAddress == email && x.IsDeleted == 0 && x.IsActive == 1);
        }

        public async Task<UserAccount?> GetByEmailAsync(string email)
        {
            return await _context.UserAccounts
                .FirstOrDefaultAsync(x => x.EmailAddress == email);
        }

        public async Task UpdateAsync(UserAccount user)
        {
            _context.UserAccounts.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}

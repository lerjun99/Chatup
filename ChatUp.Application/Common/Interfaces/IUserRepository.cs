using ChatUp.Domain.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IUserRepository
    {
        IQueryable<UserAccount> Query(); // 👈 Add this
        Task<List<UserAccount>> GetClientUsersAsync(int clientId);
        Task<UserAccount> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<UserAccount>> GetUsersAsync(CancellationToken cancellationToken = default); // ✅ added
        Task<List<UserAccount>> GetUsersByClientAsync(int clientId, CancellationToken cancellationToken = default); // 👈 add this
        Task<UserAccount?> GetUserWithDetailsAsync(int userId, CancellationToken cancellationToken);
        Task<List<UserAccount>> GetAllUsersWithDetailsAsync(CancellationToken cancellationToken);
        Task<bool> EmailExistsAsync(string email);
        Task<UserAccount?> GetByEmailAsync(string email);
        Task UpdateAsync(UserAccount user);
    }
}

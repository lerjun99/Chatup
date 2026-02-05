using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface ILoginHistoryRepository
    {
        Task AddAsync(LoginHistory history, CancellationToken cancellationToken = default);
        Task<LoginHistory?> GetLastLoginAsync(int userId, CancellationToken cancellationToken = default);
        Task UpdateAsync(LoginHistory history, CancellationToken cancellationToken = default);
    }
}

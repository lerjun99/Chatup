using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Interfaces;
public interface ITicketRepository
{
    IQueryable<Ticket> Query();
    Task<Ticket> AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int?> GetTicketRatingAsync(int ticketId, CancellationToken ct = default);
    Task<List<Ticket>> GetOpenTicketsAsync();
    Task AddHistoryAsync(TicketHistory history, CancellationToken ct = default);
    Task<IEnumerable<Ticket>> GetBreachedTicketsAsync(); // ✅ add this
    Task<IEnumerable<Ticket>> QueryAsync(Func<IQueryable<Ticket>, IQueryable<Ticket>>? query = null, CancellationToken ct = default);
}

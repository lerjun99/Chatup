using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence.Repositories;
public class TicketRepository : ITicketRepository
{
    private readonly ChatDBContext _db;
    public TicketRepository(ChatDBContext db) => _db = db;

    public async Task<Ticket> AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);

        return ticket; // entity now has Id + computed TicketNo
    }
    public IQueryable<Ticket> Query() => _db.Tickets.AsQueryable();
    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Tickets.FindAsync(new object[] { id }, ct);
        if (e == null) return;

        // Perform a soft delete
        e.IsArchived = true;
        e.DateUpdated = DateTime.UtcNow; // optional, if you have a DateUpdated column
        _db.Tickets.Update(e);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.AsNoTracking()
            .Include(t => t.RequestedBy)
            .Include(t => t.Client)
            .Include(t => t.Project)
            .Include(t => t.SupportedBy)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        Console.WriteLine(ticket?.Description); // Should output DB value
        return ticket;
    }

    public Task<IEnumerable<Ticket>> QueryAsync(Func<IQueryable<Ticket>, IQueryable<Ticket>>? query = null, CancellationToken ct = default)
    {
        IQueryable<Ticket> q = _db.Tickets.AsNoTracking();
        if (query != null) q = query(q);
        return Task.FromResult<IEnumerable<Ticket>>(q.ToList());
    }
    public async Task<List<Ticket>> GetOpenTicketsAsync()
    {
        return await _db.Tickets.AsNoTracking()
            .Where(t => !t.IsBreached) // or filter by Status == "Open"
            .ToListAsync();
    }
    public async Task<int?> GetTicketRatingAsync(int ticketId, CancellationToken ct = default)
    {
        return await _db.TicketRatings
            .AsNoTracking()
            .Where(t => t.TicketId == ticketId)
            .Select(t => t.Rating) // Assuming Rating is an int? property on Ticket
            .FirstOrDefaultAsync(ct);
    }
    public async Task<IEnumerable<Ticket>> GetBreachedTicketsAsync()
    {
        var now = DateTime.UtcNow;

        return await _db.Tickets.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Open
                        && !t.IsArchived
                        && t.DueDate < now)
            .ToListAsync();
    }
    public async Task AddHistoryAsync(TicketHistory history, CancellationToken ct = default)
    {
        _db.TicketHistories.Add(history);
        await _db.SaveChangesAsync(ct);
    }
}
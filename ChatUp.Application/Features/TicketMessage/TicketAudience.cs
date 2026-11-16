using ChatUp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.TicketMessage
{
    /// <summary>Who takes part in a ticket's chat, used for realtime delivery and sender rules.</summary>
    public static class TicketAudience
    {
        public const int Admin = 1;
        public const int Support = 2;
        public const int Client = 3;
        public const int Developer = 4;

        /// <summary>Staff write as support; everyone else (client accounts) writes as the client side.</summary>
        public static bool IsStaff(int? userType) => userType is Admin or Support or Developer;

        /// <summary>
        /// Everyone who should see a new message of the ticket live, wherever they are in the app: every support agent
        /// assigned to the ticket's client, the client's own accounts, the requester, the agent on the ticket and admins.
        /// </summary>
        public static async Task<List<int>> GetUserIdsAsync(IChatDBContext db, int ticketId, CancellationToken ct)
        {
            var ticket = await db.Tickets
                .AsNoTracking()
                .Where(t => t.Id == ticketId)
                .Select(t => new { t.ClientId, t.RequestedById, t.SupportedById })
                .FirstOrDefaultAsync(ct);

            if (ticket == null)
                return new List<int>();

            var clientId = ticket.ClientId;

            var ids = await db.UserAccounts
                .AsNoTracking()
                .Where(u => u.IsDeleted == 0 && u.Id.HasValue && (
                    u.UserType == Admin ||
                    (clientId != null && u.ClientId == clientId) ||
                    (clientId != null && db.UserClientAssignments.Any(a => a.ClientId == clientId && a.UserId == u.Id))))
                .Select(u => u.Id!.Value)
                .ToListAsync(ct);

            if (ticket.RequestedById is int requester)
                ids.Add(requester);
            if (ticket.SupportedById is int supporter)
                ids.Add(supporter);

            return ids.Distinct().ToList();
        }
    }
}

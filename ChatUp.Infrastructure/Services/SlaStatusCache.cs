using System.Collections.Concurrent;

namespace ChatUp.Infrastructure.Services
{
    /// <summary>
    /// Singleton in-memory cache holding the last-known SLA status and formatted time
    /// for every active ticket. Shared between SlaBackgroundService (writer) and
    /// SlaHub (reader), so new SignalR connections get instant data without a DB hit.
    /// </summary>
    public sealed class SlaStatusCache
    {
        public sealed record SlaEntry(string Status, string Time);

        private readonly ConcurrentDictionary<int, SlaEntry> _entries = new();

        public SlaEntry? Get(int ticketId)
            => _entries.TryGetValue(ticketId, out var e) ? e : null;

        public void Set(int ticketId, string status, string time)
            => _entries[ticketId] = new SlaEntry(status, time);

        public void Remove(int ticketId)
            => _entries.TryRemove(ticketId, out _);

        /// <summary>
        /// Returns <c>true</c> when the stored status differs from <paramref name="newStatus"/>.
        /// Returns <c>false</c> on the very first call for a ticket (seeds without broadcasting).
        /// </summary>
        public bool TryDetectChange(int ticketId, string newStatus, out string previousStatus)
        {
            if (_entries.TryGetValue(ticketId, out var existing))
            {
                previousStatus = existing.Status;
                return existing.Status != newStatus;
            }

            previousStatus = string.Empty;
            return false; // first observation — seed cache silently
        }
    }
}

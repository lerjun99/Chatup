using ChatUp.Domain.Entities;

namespace ChatUp.Application.Auditing
{
    /// <summary>
    /// Per-request audit context: who is calling, from where, and the data changes / handler notes collected so far.
    /// Filled by the API middleware, read by the MediatR behavior and the EF interceptor.
    /// </summary>
    public interface IActivityScope
    {
        Guid CorrelationId { get; set; }
        string? SessionId { get; set; }
        int? UserId { get; set; }
        string? UserName { get; set; }
        int? UserType { get; set; }
        string? IpAddress { get; set; }
        string? UserAgent { get; set; }
        string? Route { get; set; }
        string? HttpMethod { get; set; }
        string? Source { get; set; }

        /// <summary>How many audited commands are running (nested mediator.Send calls).</summary>
        int CommandDepth { get; set; }

        int ChangeCount { get; }
        int NoteCount { get; }

        void AddChanges(IEnumerable<EntityChange> changes);
        void AddNote(AuditNote note);

        /// <summary>Removes and returns everything recorded since the given positions.</summary>
        (List<EntityChange> Changes, List<AuditNote> Notes) Take(int changesFrom = 0, int notesFrom = 0);
    }

    public sealed class ActivityScope : IActivityScope
    {
        private static readonly AsyncLocal<IActivityScope?> _current = new();

        /// <summary>The scope of the request / command running on this async flow (for <see cref="AuditTrail"/>).</summary>
        public static IActivityScope? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        private readonly object _gate = new();
        private readonly List<EntityChange> _changes = new();
        private readonly List<AuditNote> _notes = new();

        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public string? SessionId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? UserType { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Route { get; set; }
        public string? HttpMethod { get; set; }
        public string? Source { get; set; }
        public int CommandDepth { get; set; }

        public int ChangeCount { get { lock (_gate) return _changes.Count; } }
        public int NoteCount { get { lock (_gate) return _notes.Count; } }

        public void AddChanges(IEnumerable<EntityChange> changes)
        {
            lock (_gate) _changes.AddRange(changes);
        }

        public void AddNote(AuditNote note)
        {
            lock (_gate) _notes.Add(note);
        }

        public (List<EntityChange> Changes, List<AuditNote> Notes) Take(int changesFrom = 0, int notesFrom = 0)
        {
            lock (_gate)
            {
                changesFrom = Math.Clamp(changesFrom, 0, _changes.Count);
                notesFrom = Math.Clamp(notesFrom, 0, _notes.Count);

                var changes = _changes.GetRange(changesFrom, _changes.Count - changesFrom);
                var notes = _notes.GetRange(notesFrom, _notes.Count - notesFrom);

                _changes.RemoveRange(changesFrom, changes.Count);
                _notes.RemoveRange(notesFrom, notes.Count);

                return (changes, notes);
            }
        }
    }

    /// <summary>Hands finished rows to the background writer so requests never wait on the audit log.</summary>
    public interface IActivityLogQueue
    {
        bool Enqueue(AdminActivityLog log);
    }

    /// <summary>
    /// Lets a handler describe what it did (summary line, record id). Inside a command the note is folded into the
    /// command's single audit row, which also carries the exact field changes the EF interceptor captured.
    /// </summary>
    public static class AuditTrail
    {
        public static void Note(string module, object? refId, string message)
        {
            ActivityScope.Current?.AddNote(new AuditNote(module, refId?.ToString(), message, Array.Empty<FieldChange>()));
        }
    }
}

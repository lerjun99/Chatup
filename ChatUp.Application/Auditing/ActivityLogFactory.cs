using ChatUp.Domain.Entities;

namespace ChatUp.Application.Auditing
{
    /// <summary>Builds <see cref="AdminActivityLog"/> rows with the caller details taken from the current scope.</summary>
    public static class ActivityLogFactory
    {
        public static AdminActivityLog Create(IActivityScope? scope, string category, string action, string module, string summary)
        {
            return new AdminActivityLog
            {
                OccurredAt = DateTime.UtcNow,
                CorrelationId = scope?.CorrelationId ?? Guid.NewGuid(),
                SessionId = string.IsNullOrEmpty(scope?.SessionId) ? null : ChangeFormatter.Truncate(scope!.SessionId, 64),
                UserId = scope?.UserId,
                UserName = string.IsNullOrWhiteSpace(scope?.UserName) ? null : ChangeFormatter.Truncate(scope!.UserName, 256),
                Category = category,
                Action = ChangeFormatter.Truncate(action, 50),
                Module = ChangeFormatter.Truncate(string.IsNullOrWhiteSpace(module) ? "General" : module, 100),
                Summary = ChangeFormatter.Truncate(summary, 500),
                Route = string.IsNullOrEmpty(scope?.Route) ? null : ChangeFormatter.Truncate(scope!.Route, 500),
                HttpMethod = scope?.HttpMethod,
                IpAddress = string.IsNullOrEmpty(scope?.IpAddress) ? null : ChangeFormatter.Truncate(scope!.IpAddress, 45),
                UserAgent = string.IsNullOrEmpty(scope?.UserAgent) ? null : ChangeFormatter.Truncate(scope!.UserAgent, 400),
                Source = scope?.Source ?? ActivitySources.System,
                IsSuccess = true
            };
        }

        /// <summary>Applies what handlers noted plus the EF-captured changes: summary, details, record id, change list.</summary>
        public static void ApplyChanges(AdminActivityLog row, IReadOnlyList<EntityChange> changes, IReadOnlyList<AuditNote> notes)
        {
            var note = notes.FirstOrDefault();

            if (note != null)
            {
                var lines = ChangeFormatter.StripChangeBlock(note.Message)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (lines.Length > 0)
                    row.Summary = ChangeFormatter.Truncate(lines[0], 500);

                var details = lines.Skip(1)
                    .Concat(notes.Skip(1).Select(n => ChangeFormatter.StripChangeBlock(n.Message)))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (details.Count > 0)
                    row.Details = string.Join("\n", details);

                if (!string.IsNullOrWhiteSpace(note.Module))
                    row.Module = ChangeFormatter.Truncate(note.Module, 100);

                if (!string.IsNullOrWhiteSpace(note.RefId) && note.RefId != "0")
                {
                    row.EntityId = ChangeFormatter.Truncate(note.RefId, 100);
                    row.EntityName ??= row.Module;
                }
            }

            row.Changes = ActivityJson.Serialize(changes);

            if (row.EntityId == null && changes.Count > 0)
            {
                var primary = changes[0];
                row.EntityName = ChangeFormatter.Truncate(primary.Entity, 100);
                row.EntityId = string.IsNullOrEmpty(primary.EntityId) ? null : ChangeFormatter.Truncate(primary.EntityId, 100);
            }
        }

        public static string PastTense(string action) => action switch
        {
            ActivityActions.Create => "Created",
            ActivityActions.Update => "Updated",
            ActivityActions.Delete => "Deleted",
            ActivityActions.Restore => "Restored",
            _ => ChangeFormatter.Humanize(action)
        };

        /// <summary>Create / Update / Delete / Restore for a set of EF changes (decided by the first, i.e. main, record).</summary>
        public static string ActionFor(IReadOnlyList<EntityChange> changes) =>
            changes.Count == 0
                ? ActivityActions.Update
                : changes[0].Action switch
                {
                    ActivityActions.Create => ActivityActions.Create,
                    ActivityActions.Delete => ActivityActions.Delete,
                    ActivityActions.Restore => ActivityActions.Restore,
                    _ => ActivityActions.Update
                };
    }
}

using ChatUp.Application.Auditing;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace ChatUp.Infrastructure.Auditing
{
    /// <summary>
    /// Captures every insert / update / delete that goes through EF Core, with the exact old and new value of each
    /// changed column, and hands them to the request's <see cref="IActivityScope"/>. The MediatR behavior (or the API
    /// middleware for writes outside MediatR) turns them into one audit row.
    ///
    /// Records updated with Attach + State = Modified leave EF without the real previous values; for those the
    /// current database row is read first so the log shows what actually changed.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        // Upper bound on extra "read the current row" lookups per SaveChanges
        private const int MaxDatabaseLookups = 50;

        // Bookkeeping tables, not user data (and the audit log itself)
        private static readonly HashSet<string> ExcludedEntities = new(StringComparer.Ordinal)
        {
            nameof(AdminActivityLog), nameof(ActivityLog), nameof(UserNotification), nameof(TicketInteraction),
            nameof(LoginHistory), nameof(LoginAttempt), nameof(ApiTokenModel), nameof(EmailOtp), nameof(ApiKey)
        };

        private readonly IActivityScope _scope;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;
        private List<(EntityEntry Entry, EntityChange Change)>? _pending;

        public AuditSaveChangesInterceptor(IActivityScope scope, ILogger<AuditSaveChangesInterceptor> logger)
        {
            _scope = scope;
            _logger = logger;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _pending = eventData.Context == null ? null : await SafeCaptureAsync(eventData.Context, allowLookups: true, cancellationToken);
            return result;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _pending = eventData.Context == null ? null : SafeCaptureAsync(eventData.Context, allowLookups: false, CancellationToken.None).GetAwaiter().GetResult();
            return result;
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Complete();
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Complete();
            return result;
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _pending = null;
            return Task.CompletedTask;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending = null;

        private void Complete()
        {
            var pending = _pending;
            _pending = null;

            if (pending == null || pending.Count == 0)
                return;

            try
            {
                foreach (var (entry, change) in pending)
                {
                    // Database-generated keys only exist after the insert
                    if (change.Action == ActivityActions.Create)
                        change.EntityId = ReadKey(entry);
                }

                _scope.AddChanges(pending.Select(p => p.Change));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log: could not complete captured changes");
            }
        }

        private async Task<List<(EntityEntry, EntityChange)>?> SafeCaptureAsync(DbContext context, bool allowLookups, CancellationToken ct)
        {
            try
            {
                return await CaptureAsync(context, allowLookups, ct);
            }
            catch (Exception ex)
            {
                // Auditing never breaks a save
                _logger.LogError(ex, "Audit log: could not capture changes");
                return null;
            }
        }

        private static async Task<List<(EntityEntry, EntityChange)>> CaptureAsync(DbContext context, bool allowLookups, CancellationToken ct)
        {
            var result = new List<(EntityEntry, EntityChange)>();
            var lookups = 0;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => !IsExcluded(e.Metadata))
                .ToList();

            foreach (var entry in entries)
            {
                var change = new EntityChange
                {
                    Entity = ChangeFormatter.Humanize(entry.Metadata.ClrType.Name),
                    EntityId = entry.State == EntityState.Added ? null : ReadKey(entry)
                };

                var properties = entry.Properties.Where(p => Include(p.Metadata)).ToList();

                switch (entry.State)
                {
                    case EntityState.Added:
                        change.Action = ActivityActions.Create;
                        foreach (var p in properties)
                        {
                            var value = ChangeFormatter.Format(p.CurrentValue);
                            if (value != null)
                                change.Fields.Add(Field(p.Metadata, null, value));
                        }
                        break;

                    case EntityState.Deleted:
                        change.Action = ActivityActions.Delete;
                        foreach (var p in properties)
                        {
                            var value = ChangeFormatter.Format(p.OriginalValue);
                            if (value != null)
                                change.Fields.Add(Field(p.Metadata, value, null));
                        }
                        break;

                    case EntityState.Modified:
                        PropertyValues? stored = null;

                        // Attach + State = Modified marks every column modified and makes original == current
                        var everythingMarked = properties.Count > 0 && properties.All(p => p.IsModified);
                        if (everythingMarked && allowLookups && lookups < MaxDatabaseLookups)
                        {
                            stored = await entry.GetDatabaseValuesAsync(ct);
                            lookups++;
                        }

                        change.Action = SoftDeleteAction(entry, stored) ?? ActivityActions.Update;

                        foreach (var p in properties)
                        {
                            if (!p.IsModified)
                                continue;

                            var oldValue = stored != null ? stored[p.Metadata] : p.OriginalValue;
                            var newValue = p.CurrentValue;

                            if (ChangeFormatter.AreEqual(oldValue, newValue))
                                continue;

                            change.Fields.Add(Field(p.Metadata, oldValue, newValue));
                        }

                        // Saved without any real difference: nothing to report
                        if (change.Fields.Count == 0 && change.Action == ActivityActions.Update)
                            continue;
                        break;
                }

                result.Add((entry, change));
            }

            return result;
        }

        private static FieldChange Field(IProperty property, object? oldValue, object? newValue)
        {
            var masked = IsMasked(property);

            return new FieldChange
            {
                Field = property.Name,
                Label = ChangeFormatter.Humanize(property.Name),
                Old = masked ? (oldValue == null ? null : ChangeFormatter.MaskedValue) : ChangeFormatter.Format(oldValue),
                New = masked ? (newValue == null ? null : ChangeFormatter.MaskedValue + " (changed)") : ChangeFormatter.Format(newValue)
            };
        }

        /// <summary>IsDeleted / IsArchived going from off to on is a delete; the reverse is a restore.</summary>
        private static string? SoftDeleteAction(EntityEntry entry, PropertyValues? stored)
        {
            foreach (var name in new[] { "IsDeleted", "IsArchived", "DeletedAt" })
            {
                var property = entry.Metadata.FindProperty(name);
                if (property == null)
                    continue;

                var member = entry.Property(name);
                var before = stored != null ? stored[property] : member.OriginalValue;
                var after = member.CurrentValue;

                if (IsOn(before) == IsOn(after))
                    continue;

                return IsOn(after) ? ActivityActions.Delete : ActivityActions.Restore;
            }

            return null;

            // ChatUp stores some flags as int (IsDeleted = 1) and some as bool
            static bool IsOn(object? value) => value switch
            {
                null => false,
                bool b => b,
                int i => i != 0,
                DateTime => true,
                _ => true
            };
        }

        private static string? ReadKey(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null)
                return null;

            var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()).ToList();
            return values.All(string.IsNullOrEmpty) ? null : string.Join(",", values);
        }

        private static bool IsExcluded(IEntityType entityType)
        {
            var clr = entityType.ClrType;
            return ExcludedEntities.Contains(clr.Name)
                   || entityType.IsPropertyBag
                   || clr.IsDefined(typeof(AuditIgnoreAttribute), true);
        }

        private static bool Include(IProperty property)
        {
            if (property.IsPrimaryKey() || property.IsShadowProperty() || property.IsConcurrencyToken)
                return false;

            if (property.Name is "IsDeleted" or "DeletedAt")
                return false;

            if (property.PropertyInfo?.IsDefined(typeof(AuditIgnoreAttribute), true) == true)
                return false;

            return IsMasked(property) || ChangeFormatter.GetSensitivity(property.Name) != FieldSensitivity.Hidden;
        }

        private static bool IsMasked(IProperty property) =>
            property.PropertyInfo?.IsDefined(typeof(AuditMaskAttribute), true) == true
            || ChangeFormatter.GetSensitivity(property.Name) == FieldSensitivity.Masked;
    }
}

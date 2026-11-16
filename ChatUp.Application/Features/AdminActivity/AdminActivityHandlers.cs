using ChatUp.Application.Auditing;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.AdminActivity
{
    public sealed class SearchActivityLogHandler : IRequestHandler<SearchActivityLogQuery, ActivityLogPageDto>
    {
        private readonly IChatDBContext _db;
        private readonly IActivityUnlockService _unlock;
        private readonly IActivityScope _scope;

        public SearchActivityLogHandler(IChatDBContext db, IActivityUnlockService unlock, IActivityScope scope)
        {
            _db = db;
            _unlock = unlock;
            _scope = scope;
        }

        public async Task<ActivityLogPageDto> Handle(SearchActivityLogQuery request, CancellationToken ct)
        {
            // Protected details only for the administrator they were unlocked for, and only until it expires
            var includeProtected = _unlock.IsUnlocked(request.UnlockToken, _scope.UserId);

            var query = _db.Set<AdminActivityLog>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var term = request.SearchValue.Trim();

                // IP / route are protected: only searchable once unlocked, otherwise a search would reveal which
                // rows came from a given IP
                query = query.Where(x =>
                    x.Summary.Contains(term)
                    || (x.Details != null && x.Details.Contains(term))
                    || (x.UserName != null && x.UserName.Contains(term))
                    || x.Module.Contains(term)
                    || x.Action.Contains(term)
                    || (x.EntityId != null && x.EntityId == term)
                    || (includeProtected && x.Route != null && x.Route.Contains(term))
                    || (includeProtected && x.IpAddress != null && x.IpAddress == term)
                    || (x.Changes != null && x.Changes.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(request.UserName))
                query = query.Where(x => x.UserName == request.UserName);
            if (request.Modules is { Count: > 0 })
                query = query.Where(x => request.Modules.Contains(x.Module));
            if (request.Categories is { Count: > 0 })
                query = query.Where(x => request.Categories.Contains(x.Category));
            if (request.Actions is { Count: > 0 })
                query = query.Where(x => request.Actions.Contains(x.Action));
            if (request.IsSuccess.HasValue)
                query = query.Where(x => x.IsSuccess == request.IsSuccess.Value);
            if (request.DateFrom.HasValue)
                query = query.Where(x => x.OccurredAt >= request.DateFrom.Value);
            if (request.DateTo.HasValue)
                query = query.Where(x => x.OccurredAt < request.DateTo.Value);
            if (!string.IsNullOrWhiteSpace(request.EntityId))
                query = query.Where(x => x.EntityId == request.EntityId);
            if (!string.IsNullOrWhiteSpace(request.SessionId))
                query = query.Where(x => x.SessionId == request.SessionId);
            if (request.CorrelationId.HasValue)
                query = query.Where(x => x.CorrelationId == request.CorrelationId.Value);
            if (request.HidePageViews)
                query = query.Where(x => x.Category != ActivityCategories.Navigation);

            var total = await query.CountAsync(ct);

            var desc = request.SortDescending;
            query = (request.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "username" => desc ? query.OrderByDescending(x => x.UserName).ThenByDescending(x => x.Id) : query.OrderBy(x => x.UserName).ThenBy(x => x.Id),
                "module" => desc ? query.OrderByDescending(x => x.Module).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Module).ThenBy(x => x.Id),
                "action" => desc ? query.OrderByDescending(x => x.Action).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Action).ThenBy(x => x.Id),
                "category" => desc ? query.OrderByDescending(x => x.Category).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Category).ThenBy(x => x.Id),
                "durationms" => desc ? query.OrderByDescending(x => x.DurationMs).ThenByDescending(x => x.Id) : query.OrderBy(x => x.DurationMs).ThenBy(x => x.Id),
                _ => desc ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
            };

            var pageSize = Math.Clamp(request.PageSize, 1, 5000);
            var pageIndex = Math.Max(request.PageIndex, 0);

            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .Select(x => new ActivityLogDto
                {
                    Id = x.Id,
                    OccurredAt = x.OccurredAt,
                    CorrelationId = x.CorrelationId,
                    SessionId = x.SessionId,
                    UserId = x.UserId,
                    UserName = x.UserName,
                    Category = x.Category,
                    Action = x.Action,
                    Module = x.Module,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    Summary = x.Summary,
                    Details = x.Details,
                    Changes = x.Changes,
                    Route = x.Route,
                    HttpMethod = x.HttpMethod,
                    StatusCode = x.StatusCode,
                    DurationMs = x.DurationMs,
                    IsSuccess = x.IsSuccess,
                    ErrorMessage = x.ErrorMessage,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    Source = x.Source
                })
                .ToListAsync(ct);

            foreach (var row in items)
            {
                row.OccurredAt = DateTime.SpecifyKind(row.OccurredAt, DateTimeKind.Utc);
                if (!includeProtected)
                    HideProtected(row);
            }

            return new ActivityLogPageDto { Items = items, Total = total };
        }

        private static void HideProtected(ActivityLogDto row)
        {
            row.DetailsLocked = true;
            row.IpAddress = null;
            row.UserAgent = null;
            row.Route = null;
            row.HttpMethod = null;
            row.StatusCode = null;
            row.DurationMs = null;
            row.SessionId = null;
            row.CorrelationId = Guid.Empty;
            row.UserId = null;
            row.Summary = ActivityRedaction.HideRoutes(row.Summary) ?? string.Empty;
            row.Details = ActivityRedaction.HideRoutes(row.Details);
            row.ErrorMessage = ActivityRedaction.HideRoutes(row.ErrorMessage);
        }
    }

    public sealed class GetActivityLogFiltersHandler : IRequestHandler<GetActivityLogFiltersQuery, ActivityLogFiltersDto>
    {
        private readonly IChatDBContext _db;
        private readonly IActivityUnlockService _unlock;
        private readonly IActivityScope _scope;

        public GetActivityLogFiltersHandler(IChatDBContext db, IActivityUnlockService unlock, IActivityScope scope)
        {
            _db = db;
            _unlock = unlock;
            _scope = scope;
        }

        public async Task<ActivityLogFiltersDto> Handle(GetActivityLogFiltersQuery request, CancellationToken ct)
        {
            var set = _db.Set<AdminActivityLog>().AsNoTracking();

            return new ActivityLogFiltersDto
            {
                Modules = await set.Select(x => x.Module).Distinct().OrderBy(x => x).ToListAsync(ct),
                Actions = await set.Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(ct),
                Categories = ActivityCategories.All.ToList(),
                CanUnlockProtected = await _unlock.CanUnlockAsync(_scope.UserId, ct)
            };
        }
    }

    public sealed class SearchActivityUsersHandler : IRequestHandler<SearchActivityUsersQuery, List<string>>
    {
        private readonly IChatDBContext _db;

        public SearchActivityUsersHandler(IChatDBContext db) => _db = db;

        public async Task<List<string>> Handle(SearchActivityUsersQuery request, CancellationToken ct)
        {
            var query = _db.Set<AdminActivityLog>().AsNoTracking().Where(x => x.UserName != null);

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var term = request.SearchValue.Trim();
                query = query.Where(x => x.UserName!.Contains(term));
            }

            return await query.Select(x => x.UserName!).Distinct().OrderBy(x => x).Take(20).ToListAsync(ct);
        }
    }

    public sealed class UnlockActivityDetailsHandler : IRequestHandler<UnlockActivityDetailsCommand, ActivityUnlockResultDto>
    {
        private readonly IActivityUnlockService _unlock;
        private readonly IActivityScope _scope;

        public UnlockActivityDetailsHandler(IActivityUnlockService unlock, IActivityScope scope)
        {
            _unlock = unlock;
            _scope = scope;
        }

        public async Task<ActivityUnlockResultDto> Handle(UnlockActivityDetailsCommand request, CancellationToken ct)
        {
            var result = await _unlock.UnlockAsync(_scope.UserId, request.Password, ct);

            // The reason goes on this command's own audit row; the password never does
            _scope.AddNote(new AuditNote("Audit log", null,
                result.IsSuccess ? "Protected details unlocked" : $"Protected details unlock refused\nReason: {result.Message}",
                Array.Empty<FieldChange>()));

            return result;
        }
    }

    public sealed class GetActivityDashboardHandler : IRequestHandler<GetActivityDashboardQuery, ActivityDashboardDto>
    {
        private const int MaxDays = 366;
        private const int Top = 10;

        private readonly IChatDBContext _db;

        public GetActivityDashboardHandler(IChatDBContext db) => _db = db;

        public async Task<ActivityDashboardDto> Handle(GetActivityDashboardQuery request, CancellationToken ct)
        {
            var toUtc = request.DateTo ?? DateTime.UtcNow;
            var fromUtc = request.DateFrom ?? toUtc.AddDays(-30);

            if (fromUtc >= toUtc)
                fromUtc = toUtc.AddDays(-1);
            if ((toUtc - fromUtc).TotalDays > MaxDays)
                fromUtc = toUtc.AddDays(-MaxDays);

            var timeZone = ResolveTimeZone(request.TimeZoneId);
            var set = _db.Set<AdminActivityLog>().AsNoTracking();
            var range = set.Where(x => x.OccurredAt >= fromUtc && x.OccurredAt < toUtc);
            var nonNavigation = range.Where(x => x.Category != ActivityCategories.Navigation);

            // Hourly buckets (UTC) per type and outcome: small, then shaped in memory into the per-day trend and the
            // hour-of-week heatmap in the viewer's time zone
            var buckets = await range
                .GroupBy(x => new { x.OccurredAt.Date, x.OccurredAt.Hour, x.Category, x.IsSuccess })
                .Select(g => new { g.Key.Date, g.Key.Hour, g.Key.Category, g.Key.IsSuccess, Count = g.Count() })
                .ToListAsync(ct);

            var actions = await nonNavigation
                .GroupBy(x => new { x.Category, x.Action })
                .Select(g => new { g.Key.Category, g.Key.Action, Count = g.Count() })
                .ToListAsync(ct);

            var activeUsers = await range.Where(x => x.UserName != null).Select(x => x.UserName).Distinct().CountAsync(ct);

            var previousFrom = fromUtc - (toUtc - fromUtc);
            var previous = set.Where(x => x.OccurredAt >= previousFrom && x.OccurredAt < fromUtc);
            var previousTotal = await previous.CountAsync(ct);
            var previousFailures = await previous.CountAsync(x => !x.IsSuccess, ct);
            var previousUsers = await previous.Where(x => x.UserName != null).Select(x => x.UserName).Distinct().CountAsync(ct);

            var topUsers = await range.Where(x => x.UserName != null)
                .GroupBy(x => x.UserName!)
                .Select(g => new ActivityCountDto { Key = g.Key, Count = g.Count(), Failures = g.Count(x => !x.IsSuccess) })
                .OrderByDescending(x => x.Count).Take(Top)
                .ToListAsync(ct);

            var topModules = await nonNavigation
                .GroupBy(x => x.Module)
                .Select(g => new ActivityCountDto { Key = g.Key, Count = g.Count(), Failures = g.Count(x => !x.IsSuccess) })
                .OrderByDescending(x => x.Count).Take(Top)
                .ToListAsync(ct);

            var topPages = await range.Where(x => x.Category == ActivityCategories.Navigation)
                .GroupBy(x => x.Module)
                .Select(g => new ActivityCountDto { Key = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(Top)
                .ToListAsync(ct);

            var failedSignInUsers = await range.Where(x => x.Action == ActivityActions.LoginFailed && x.UserName != null)
                .GroupBy(x => x.UserName!)
                .Select(g => new ActivityCountDto { Key = g.Key, Count = g.Count(), Failures = g.Count() })
                .OrderByDescending(x => x.Count).Take(5)
                .ToListAsync(ct);

            var sources = await range
                .GroupBy(x => x.Source)
                .Select(g => new ActivityCountDto { Key = g.Key ?? "Unknown", Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            var recentFailures = await range.Where(x => !x.IsSuccess)
                .OrderByDescending(x => x.Id)
                .Take(8)
                .Select(x => new ActivityFailureDto
                {
                    Id = x.Id,
                    OccurredAt = x.OccurredAt,
                    UserName = x.UserName,
                    Category = x.Category,
                    Action = x.Action,
                    Module = x.Module,
                    Summary = x.Summary,
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync(ct);

            foreach (var failure in recentFailures)
            {
                // The dashboard never shows protected details
                failure.OccurredAt = DateTime.SpecifyKind(failure.OccurredAt, DateTimeKind.Utc);
                failure.Summary = ActivityRedaction.HideRoutes(failure.Summary) ?? string.Empty;
                failure.ErrorMessage = ActivityRedaction.HideRoutes(failure.ErrorMessage);
            }

            var dashboard = new ActivityDashboardDto
            {
                DateFrom = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc),
                DateTo = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc),
                Total = buckets.Sum(x => x.Count),
                Failures = buckets.Where(x => !x.IsSuccess).Sum(x => x.Count),
                PageViews = buckets.Where(x => x.Category == ActivityCategories.Navigation).Sum(x => x.Count),
                ActiveUsers = activeUsers,
                PreviousTotal = previousTotal,
                PreviousFailures = previousFailures,
                PreviousActiveUsers = previousUsers,
                SignIns = actions.Where(x => x.Action == ActivityActions.Login).Sum(x => x.Count),
                FailedSignIns = actions.Where(x => x.Action == ActivityActions.LoginFailed).Sum(x => x.Count),
                SignOuts = actions.Where(x => x.Action == ActivityActions.Logout).Sum(x => x.Count),
                DataChanges = buckets.Where(x => x.Category == ActivityCategories.Data && x.IsSuccess).Sum(x => x.Count),
                AccessDenied = actions.Where(x => x.Action == ActivityActions.AccessDenied).Sum(x => x.Count),
                Errors = buckets.Where(x => x.Category == ActivityCategories.System).Sum(x => x.Count),
                TopUsers = topUsers,
                TopModules = topModules,
                TopPages = topPages,
                FailedSignInUsers = failedSignInUsers,
                Sources = sources,
                RecentFailures = recentFailures
            };

            dashboard.Categories = ActivityCategories.All
                .Select(c => new ActivityCountDto
                {
                    Key = c,
                    Count = buckets.Where(x => x.Category == c).Sum(x => x.Count),
                    Failures = buckets.Where(x => x.Category == c && !x.IsSuccess).Sum(x => x.Count)
                })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToList();

            dashboard.DataActions = actions
                .Where(x => x.Category == ActivityCategories.Data)
                .GroupBy(x => x.Action)
                .Select(g => new ActivityCountDto { Key = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Every local day in the range, so quiet days show as gaps in the trend
            var firstDay = ToLocal(fromUtc, timeZone).Date;
            var lastDay = ToLocal(toUtc.AddTicks(-1), timeZone).Date;
            var days = new Dictionary<DateTime, ActivityDayStatDto>();
            for (var day = firstDay; day <= lastDay && days.Count < 400; day = day.AddDays(1))
                days[day] = new ActivityDayStatDto { Date = day };

            foreach (var bucket in buckets)
            {
                var local = ToLocal(bucket.Date.AddHours(bucket.Hour), timeZone);
                dashboard.HourOfWeek[(int)local.DayOfWeek * 24 + local.Hour] += bucket.Count;

                if (!days.TryGetValue(local.Date, out var stat))
                    continue;

                stat.Total += bucket.Count;
                if (!bucket.IsSuccess)
                    stat.Failures += bucket.Count;
                stat.ByCategory[bucket.Category] = stat.ByCategory.GetValueOrDefault(bucket.Category) + bucket.Count;
            }

            dashboard.Days = days.Values.OrderBy(x => x.Date).ToList();
            return dashboard;
        }

        private static DateTime ToLocal(DateTime utc, TimeZoneInfo timeZone) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified), timeZone);

        private static TimeZoneInfo ResolveTimeZone(string? id)
        {
            foreach (var candidate in new[] { id, "Asia/Manila", "Singapore Standard Time" })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                try { return TimeZoneInfo.FindSystemTimeZoneById(candidate); }
                catch (Exception) { }
            }
            return TimeZoneInfo.CreateCustomTimeZone("UTC+8", TimeSpan.FromHours(8), "UTC+8", "UTC+8");
        }
    }
}

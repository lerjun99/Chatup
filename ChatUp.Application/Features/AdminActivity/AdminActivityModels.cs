using MediatR;

namespace ChatUp.Application.Features.AdminActivity
{
    public class ActivityLogDto
    {
        public long Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid CorrelationId { get; set; }
        public string? SessionId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? Details { get; set; }
        /// <summary>JSON list of entity changes (ChatUp.Application.Auditing.EntityChange).</summary>
        public string? Changes { get; set; }
        public string? Route { get; set; }
        public string? HttpMethod { get; set; }
        public int? StatusCode { get; set; }
        public int? DurationMs { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Source { get; set; }

        /// <summary>True when IP, device, route, session and request ids were left out (not unlocked).</summary>
        public bool DetailsLocked { get; set; }
    }

    public class ActivityLogPageDto
    {
        public List<ActivityLogDto> Items { get; set; } = new();
        public int Total { get; set; }
    }

    public class ActivityLogFiltersDto
    {
        public List<string> Modules { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        /// <summary>Whether the caller may unlock the protected details (administrators).</summary>
        public bool CanUnlockProtected { get; set; }
    }

    public class SearchActivityLogQuery : IRequest<ActivityLogPageDto>
    {
        /// <summary>Free text: summary, details, user, module, action, record id, changed values.</summary>
        public string? SearchValue { get; set; }
        public string? UserName { get; set; }
        public List<string>? Modules { get; set; }
        public List<string>? Categories { get; set; }
        public List<string>? Actions { get; set; }
        public bool? IsSuccess { get; set; }
        /// <summary>Inclusive lower bound, UTC.</summary>
        public DateTime? DateFrom { get; set; }
        /// <summary>Exclusive upper bound, UTC.</summary>
        public DateTime? DateTo { get; set; }
        public string? EntityId { get; set; }
        public string? SessionId { get; set; }
        public Guid? CorrelationId { get; set; }
        public bool HidePageViews { get; set; }
        /// <summary>OccurredAt | UserName | Module | Action | Category | DurationMs</summary>
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        public int PageIndex { get; set; }
        public int PageSize { get; set; } = 25;

        /// <summary>From AdminActivity/Unlock: without a valid one the protected details are left out.</summary>
        public string? UnlockToken { get; set; }
    }

    public record GetActivityLogFiltersQuery : IRequest<ActivityLogFiltersDto>;

    public record SearchActivityUsersQuery(string? SearchValue) : IRequest<List<string>>;

    /// <summary>The signed-in administrator re-enters their password to see the protected details.</summary>
    public class UnlockActivityDetailsCommand : IRequest<ActivityUnlockResultDto>
    {
        public string Password { get; set; } = string.Empty;
    }

    public class ActivityUnlockResultDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }

    /// <summary>Issues and checks the short-lived "protected details unlocked" tokens.</summary>
    public interface IActivityUnlockService
    {
        Task<ActivityUnlockResultDto> UnlockAsync(int? userId, string password, CancellationToken ct);
        bool IsUnlocked(string? token, int? userId);
        Task<bool> CanUnlockAsync(int? userId, CancellationToken ct);
    }

    // ---------------- Dashboard ----------------

    public class GetActivityDashboardQuery : IRequest<ActivityDashboardDto>
    {
        /// <summary>Inclusive lower bound, UTC. Defaults to the last 30 days.</summary>
        public DateTime? DateFrom { get; set; }
        /// <summary>Exclusive upper bound, UTC. Defaults to now.</summary>
        public DateTime? DateTo { get; set; }
        /// <summary>The viewer's time zone; days and hours are grouped in it. Philippine time by default.</summary>
        public string? TimeZoneId { get; set; }
    }

    public class ActivityDashboardDto
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        public int Total { get; set; }
        public int PreviousTotal { get; set; }
        public int ActiveUsers { get; set; }
        public int PreviousActiveUsers { get; set; }
        public int SignIns { get; set; }
        public int FailedSignIns { get; set; }
        public int SignOuts { get; set; }
        public int DataChanges { get; set; }
        public int Failures { get; set; }
        public int PreviousFailures { get; set; }
        public int AccessDenied { get; set; }
        public int Errors { get; set; }
        public int PageViews { get; set; }

        /// <summary>One entry per local day in the range, including days without activity.</summary>
        public List<ActivityDayStatDto> Days { get; set; } = new();

        /// <summary>7 x 24 counts, index = (int)DayOfWeek * 24 + hour, in the viewer's time zone.</summary>
        public int[] HourOfWeek { get; set; } = new int[7 * 24];

        public List<ActivityCountDto> Categories { get; set; } = new();
        public List<ActivityCountDto> DataActions { get; set; } = new();
        public List<ActivityCountDto> Sources { get; set; } = new();
        public List<ActivityCountDto> TopUsers { get; set; } = new();
        public List<ActivityCountDto> TopModules { get; set; } = new();
        public List<ActivityCountDto> TopPages { get; set; } = new();
        public List<ActivityCountDto> FailedSignInUsers { get; set; } = new();
        public List<ActivityFailureDto> RecentFailures { get; set; } = new();
    }

    public class ActivityDayStatDto
    {
        public DateTime Date { get; set; }
        public int Total { get; set; }
        public int Failures { get; set; }
        public Dictionary<string, int> ByCategory { get; set; } = new();
    }

    public class ActivityCountDto
    {
        public string Key { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Failures { get; set; }
    }

    public class ActivityFailureDto
    {
        public long Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? UserName { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>Sent by the web app on every page change.</summary>
    public class LogPageViewRequest
    {
        public string Route { get; set; } = string.Empty;
        public string? Title { get; set; }
    }
}

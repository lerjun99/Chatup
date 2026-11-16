using ChatUp.Application.Features.AdminActivity;

namespace ChatUp.Services
{
    /// <summary>Calls the API's audit log endpoints (administrators only — the API checks the signed-in session).</summary>
    public sealed class AuditLogService
    {
        private readonly HttpClient _http;

        public AuditLogService(HttpClient http) => _http = http;

        private static string Url(string action) => $"{AppConfig.ChatUrl}AdminActivity/{action}";

        public async Task<ActivityLogPageDto> SearchAsync(SearchActivityLogQuery query, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(Url("Search"), query, ct);
            await EnsureAsync(response);
            return await response.Content.ReadFromJsonAsync<ActivityLogPageDto>(cancellationToken: ct) ?? new();
        }

        public async Task<ActivityLogFiltersDto> GetFiltersAsync(CancellationToken ct = default)
        {
            var response = await _http.GetAsync(Url("Filters"), ct);
            await EnsureAsync(response);
            return await response.Content.ReadFromJsonAsync<ActivityLogFiltersDto>(cancellationToken: ct) ?? new();
        }

        public async Task<List<string>> SearchUsersAsync(string? search, CancellationToken ct = default) =>
            await _http.GetFromJsonAsync<List<string>>(Url($"Users?search={Uri.EscapeDataString(search ?? string.Empty)}"), ct) ?? new();

        public async Task<ActivityDashboardDto> GetDashboardAsync(GetActivityDashboardQuery query, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(Url("Dashboard"), query, ct);
            await EnsureAsync(response);
            return await response.Content.ReadFromJsonAsync<ActivityDashboardDto>(cancellationToken: ct) ?? new();
        }

        public async Task<ActivityUnlockResultDto> UnlockAsync(string password, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(Url("Unlock"), new UnlockActivityDetailsCommand { Password = password }, ct);
            await EnsureAsync(response);
            return await response.Content.ReadFromJsonAsync<ActivityUnlockResultDto>(cancellationToken: ct)
                   ?? new ActivityUnlockResultDto { IsSuccess = false, Message = "No response." };
        }

        private static async Task EnsureAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized =>
                    new UnauthorizedAccessException("Only a signed-in administrator can view the audit log. Sign in again if your session expired."),
                _ => new HttpRequestException($"The audit log could not be loaded ({(int)response.StatusCode}). {await response.Content.ReadAsStringAsync()}".Trim())
            };
        }
    }

    /// <summary>The audit log is read in Philippine time, whatever the web server's own clock is.</summary>
    public static class AuditTime
    {
        private static readonly TimeZoneInfo Zone = Resolve();

        public static DateTime ToLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

        public static DateTime ToUtc(DateTime local) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

        public static DateTime Today => ToLocal(DateTime.UtcNow).Date;

        public static string ZoneId => Zone.Id;

        public static string Relative(DateTime utc)
        {
            var minutes = (DateTime.UtcNow - DateTime.SpecifyKind(utc, DateTimeKind.Utc)).TotalMinutes;
            if (minutes < 1) return "just now";
            if (minutes < 60) return $"{(int)minutes}m ago";
            if (minutes < 1440) return $"{(int)(minutes / 60)}h ago";
            if (minutes < 10080) return $"{(int)(minutes / 1440)}d ago";
            return ToLocal(utc).ToString("MMM d, yyyy");
        }

        private static TimeZoneInfo Resolve()
        {
            foreach (var id in new[] { "Asia/Manila", "Singapore Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (Exception) { }
            }
            return TimeZoneInfo.CreateCustomTimeZone("UTC+8", TimeSpan.FromHours(8), "UTC+8", "UTC+8");
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using ChatUp.Application.Common.Security;

namespace ChatUp.Services.Security
{
    public sealed class ApiKeyView
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsBuiltIn { get; set; }
        public bool LabelShowsKey { get; set; }
        public bool IsLegacy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? RotatedAt { get; set; }
    }

    public sealed class ApiKeySecretView
    {
        public ApiKeyView Key { get; set; } = new();
        public string Value { get; set; } = string.Empty;
    }

    public sealed record ApiResult<T>(T? Data, string? Error)
    {
        public bool Ok => Error == null;
        public static ApiResult<T> Success(T? data) => new(data, null);
        public static ApiResult<T> Fail(string error) => new(default, error);
    }

    /// <summary>
    /// Calls the API key management endpoints for the Settings page. Admin endpoints also send the
    /// signed-in user's session token (X-User-Token) so the API can verify they're an administrator.
    /// </summary>
    public sealed class ApiKeyAdminService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly ISessionStorageService _session;
        private readonly ILocalStorageService _local;
        private readonly LinkService _links;

        public ApiKeyAdminService(HttpClient http, ISessionStorageService session, ILocalStorageService local, LinkService links)
        {
            _http = http;
            _session = session;
            _local = local;
            _links = links;
        }

        private static string Url(string path) => $"{AppConfig.ChatUrl}ApiKeys/{path}";

        /// <summary>Identifies a key. With <paramref name="keyOverride"/> it tests that key instead of this UI's key.</summary>
        public Task<ApiResult<ApiKeyView>> WhoAmIAsync(string? keyOverride = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Url("WhoAmI"));
            if (!string.IsNullOrWhiteSpace(keyOverride))
                request.Headers.TryAddWithoutValidation(ApiKeyDefaults.HeaderName, keyOverride.Trim());

            return SendAsync<ApiKeyView>(request, admin: false);
        }

        public Task<ApiResult<List<ApiKeyView>>> ListAsync() =>
            SendAsync<List<ApiKeyView>>(new HttpRequestMessage(HttpMethod.Get, Url("GetAll")), admin: true);

        public Task<ApiResult<ApiKeySecretView>> CreateAsync(string name, int role, int? expiresInDays) =>
            SendAsync<ApiKeySecretView>(new HttpRequestMessage(HttpMethod.Post, Url("Create"))
            {
                Content = JsonContent.Create(new { name, role, expiresInDays })
            }, admin: true);

        public Task<ApiResult<ApiKeySecretView>> RevealAsync(int id) =>
            SendAsync<ApiKeySecretView>(new HttpRequestMessage(HttpMethod.Get, Url($"Reveal/{id}")), admin: true);

        public Task<ApiResult<ApiKeyView>> UpdateAsync(int id, string name, int? expiresInDays, bool clearExpiry) =>
            SendAsync<ApiKeyView>(new HttpRequestMessage(HttpMethod.Put, Url($"Update/{id}"))
            {
                Content = JsonContent.Create(new { name, expiresInDays, clearExpiry })
            }, admin: true);

        public Task<ApiResult<ApiKeySecretView>> RotateAsync(int id) =>
            SendAsync<ApiKeySecretView>(new HttpRequestMessage(HttpMethod.Post, Url($"Rotate/{id}")), admin: true);

        public Task<ApiResult<bool>> RevokeAsync(int id) =>
            SendAsync<bool>(new HttpRequestMessage(HttpMethod.Delete, Url($"Revoke/{id}")), admin: true);

        private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, bool admin)
        {
            try
            {
                if (admin)
                {
                    var userToken = await GetUserTokenAsync();
                    if (userToken == null)
                        return ApiResult<T>.Fail("Your session has expired. Sign in again to manage API keys.");
                    request.Headers.TryAddWithoutValidation(ApiKeyDefaults.UserTokenHeader, userToken);
                }

                using var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(bool))
                        return ApiResult<T>.Success((T)(object)true);

                    return ApiResult<T>.Success(await response.Content.ReadFromJsonAsync<T>(JsonOptions));
                }

                return ApiResult<T>.Fail(await DescribeErrorAsync(response));
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Fail("Can't reach the API. Check that it's running and the URL in appconfig.json is correct.");
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Fail($"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<string> DescribeErrorAsync(HttpResponseMessage response)
        {
            string? message = null;
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    message = m.GetString();
            }
            catch
            {
                // body isn't JSON
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => message ?? "The API rejected the key (missing, revoked or expired).",
                HttpStatusCode.Forbidden => "Only administrators (or an Admin key) can manage API keys.",
                HttpStatusCode.NotFound => "That key no longer exists.",
                _ => message ?? $"The API returned {(int)response.StatusCode}."
            };
        }

        private async Task<string?> GetUserTokenAsync()
        {
            try
            {
                var stored = await _session.GetItemAsync<string>("AuthToken");
                if (string.IsNullOrEmpty(stored))
                    stored = await _local.GetItemAsync<string>("AuthToken");
                if (string.IsNullOrEmpty(stored))
                    return null;

                var token = _links.Unprotect(Uri.UnescapeDataString(stored));
                return string.IsNullOrWhiteSpace(token) ? null : token;
            }
            catch
            {
                return null;
            }
        }
    }
}

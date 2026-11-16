using System.Net.Http.Json;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using ChatUp.Application.Common.Security;

namespace ChatUp.Services.Security
{
    public enum SessionStatus
    {
        /// <summary>Signed in and the API confirms the session is still active.</summary>
        Active,
        /// <summary>Nobody is signed in on this browser.</summary>
        SignedOut,
        /// <summary>A token is stored but the API no longer accepts it (logged out, expired, signed in elsewhere, deactivated).</summary>
        Expired,
        /// <summary>The API could not be reached, so the session can't be confirmed (pages stay hidden).</summary>
        Unavailable
    }

    public sealed record SessionCheck(SessionStatus Status, int? UserId = null, int? UserType = null, string? Username = null, int? ClientId = null);

    /// <summary>
    /// The one place that decides whether this browser is signed in. MainLayout asks it before rendering any page and
    /// again before every navigation, so routes can't be opened from a saved/copied link once the session is gone.
    /// The stored token is only a claim: the API is asked every time whether that session is still active.
    /// </summary>
    public sealed class SessionGuard
    {
        // Every value Login.razor stores; "remember me" keeps them in localStorage, otherwise sessionStorage
        private static readonly string[] SessionKeys = { "UserId", "Username", "UserType", "AuthToken", "AvatarUrl", "ClientId" };

        private readonly ISessionStorageService _session;
        private readonly ILocalStorageService _local;
        private readonly LinkService _links;
        private readonly ActivityIdentity _identity;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<SessionGuard> _logger;

        public SessionGuard(
            ISessionStorageService session,
            ILocalStorageService local,
            LinkService links,
            ActivityIdentity identity,
            IHttpClientFactory httpFactory,
            ILogger<SessionGuard> logger)
        {
            _session = session;
            _local = local;
            _links = links;
            _identity = identity;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        /// <summary>The last confirmed result (null until the first check).</summary>
        public SessionCheck? Current { get; private set; }

        public async Task<SessionCheck> CheckAsync(CancellationToken cancellationToken = default)
        {
            var token = await ReadTokenAsync();
            if (string.IsNullOrEmpty(token))
                return Current = new SessionCheck(SessionStatus.SignedOut);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConfig.ChatUrl}Auth/Session/session");
                request.Headers.TryAddWithoutValidation(ApiKeyDefaults.UserTokenHeader, token);

                using var response = await _httpFactory.CreateClient().SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Session check returned {Status}", (int)response.StatusCode);
                    return Current = new SessionCheck(SessionStatus.Unavailable);
                }

                var info = await response.Content.ReadFromJsonAsync<SessionInfo>(cancellationToken: cancellationToken);
                if (info is not { Active: true, UserId: not null })
                {
                    await ClearAsync();
                    return Current = new SessionCheck(SessionStatus.Expired);
                }

                _identity.UserToken = token;
                return Current = new SessionCheck(SessionStatus.Active, info.UserId, info.UserType, info.Username, info.ClientId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session check failed");
                return Current = new SessionCheck(SessionStatus.Unavailable);
            }
        }

        /// <summary>Forgets the session in this browser (both storages) — used when the API says it is no longer valid.</summary>
        public async Task ClearAsync()
        {
            _identity.UserToken = null;
            foreach (var key in SessionKeys)
            {
                try { await _session.RemoveItemAsync(key); } catch { /* storage unavailable */ }
                try { await _local.RemoveItemAsync(key); } catch { /* storage unavailable */ }
            }
        }

        /// <summary>
        /// The stored session token. A "remember me" sign-in lives in localStorage; it is copied into this tab's
        /// sessionStorage so the rest of the app (UserState and pages read sessionStorage) sees the same user.
        /// </summary>
        private async Task<string?> ReadTokenAsync()
        {
            try
            {
                var stored = await _session.GetItemAsync<string>("AuthToken");
                if (string.IsNullOrEmpty(stored))
                {
                    stored = await _local.GetItemAsync<string>("AuthToken");
                    if (string.IsNullOrEmpty(stored))
                        return null;

                    foreach (var key in SessionKeys)
                    {
                        var value = await _local.GetItemAsync<string>(key);
                        if (!string.IsNullOrEmpty(value))
                            await _session.SetItemAsync(key, value);
                    }
                }

                var token = _links.Unprotect(Uri.UnescapeDataString(stored));
                return string.IsNullOrWhiteSpace(token) ? null : token;
            }
            catch (Exception ex)
            {
                // Unreadable (tampered, or the data-protection keys changed): treat as signed out
                _logger.LogDebug(ex, "Stored session token could not be read");
                return null;
            }
        }

        private sealed class SessionInfo
        {
            public bool Active { get; set; }
            public int? UserId { get; set; }
            public int? UserType { get; set; }
            public string? Username { get; set; }
            public int? ClientId { get; set; }
        }
    }
}

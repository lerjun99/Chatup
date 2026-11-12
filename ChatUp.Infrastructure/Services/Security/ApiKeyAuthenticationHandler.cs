using System.Security.Claims;
using System.Text.Encodings.Web;
using ChatUp.Application.Common.Security;
using ChatUp.Domain.Entities;
using ChatUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatUp.Infrastructure.Services.Security
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>How long a validated key is trusted from memory before re-checking the database.</summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Authenticates every API request by its API key:
    ///   X-Api-Key: key            (UI and integrations)
    ///   Authorization: Bearer key (integrations, SignalR .NET clients)
    ///   ?access_token=key         (SignalR browser/WebSocket clients)
    /// Keys are looked up by SHA-256 hash and cached briefly, so a request costs no DB round-trip.
    /// When the caller also sends X-User-Token (the signed-in user's session token),
    /// the user's id and type are added as claims for admin-only endpoints.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private static readonly TimeSpan LastUsedWriteInterval = TimeSpan.FromMinutes(1);

        private readonly ChatDBContext _db;
        private readonly IMemoryCache _cache;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ChatDBContext db,
            IMemoryCache cache)
            : base(options, logger, encoder)
        {
            _db = db;
            _cache = cache;
        }

        /// <summary>Snapshot of a valid key kept in memory.</summary>
        public sealed record CachedKey(int Id, string Name, string Role, DateTime? ExpiresAt, bool IsLegacy);

        public static string CacheKeyFor(string hash) => $"apikey:{hash}";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var providedKey = ReadProvidedKey();
            if (string.IsNullOrWhiteSpace(providedKey))
                return AuthenticateResult.NoResult();

            var hash = ApiKeyDefaults.Hash(providedKey);
            var key = await _cache.GetOrCreateAsync(CacheKeyFor(hash), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = Options.CacheDuration;
                return await FindKeyAsync(providedKey, hash);
            });

            if (key == null || (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow))
            {
                Logger.LogWarning("Rejected API key {Preview} from {Ip}",
                    ApiKeyDefaults.Mask(providedKey), Context.Connection.RemoteIpAddress);
                return AuthenticateResult.Fail("Invalid, expired or revoked API key.");
            }

            if (!key.IsLegacy)
                await TouchLastUsedAsync(key.Id);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, key.Name),
                new(ClaimTypes.Role, key.Role),
                new(ApiKeyDefaults.KeyIdClaim, key.Id.ToString()),
                new(ApiKeyDefaults.KeyNameClaim, key.Name)
            };

            await AddUserClaimsAsync(claims);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        private string? ReadProvidedKey()
        {
            if (Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
                return apiKey.ToString().Trim();

            var authorization = Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authorization["Bearer ".Length..].Trim();

            // SignalR clients that can't set headers pass the key as access_token
            if (Request.Query.TryGetValue("access_token", out var accessToken) && !string.IsNullOrWhiteSpace(accessToken))
                return accessToken.ToString().Trim();

            return null;
        }

        private async Task<CachedKey?> FindKeyAsync(string providedKey, string hash)
        {
            var key = await _db.ApiKeys
                .AsNoTracking()
                .Where(k => k.KeyHash == hash && k.IsActive)
                .Select(k => new CachedKey(k.Id, k.Name, k.Role.ToString(), k.ExpiresAt, false))
                .FirstOrDefaultAsync();

            if (key != null)
                return key;

            // Backward compatibility: tokens from the original ApiTokenModel table keep working
            var legacy = await _db.ApiTokenModel
                .AsNoTracking()
                .Where(t => t.ApiToken == providedKey && t.Status == 1)
                .Select(t => new { t.Id, t.Name, t.Role })
                .FirstOrDefaultAsync();

            if (legacy == null)
                return null;

            var role = string.Equals(legacy.Role, "Administrators", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(legacy.Role, nameof(ApiKeyRole.Admin), StringComparison.OrdinalIgnoreCase)
                ? nameof(ApiKeyRole.Admin)
                : nameof(ApiKeyRole.ApiUser);

            return new CachedKey(-legacy.Id, legacy.Name ?? "Legacy token", role, null, true);
        }

        // "Last used" is informational; write it at most once a minute per key
        private async Task TouchLastUsedAsync(int keyId)
        {
            var throttleKey = $"apikey:touched:{keyId}";
            if (_cache.TryGetValue(throttleKey, out _))
                return;

            _cache.Set(throttleKey, true, LastUsedWriteInterval);

            try
            {
                var now = DateTime.UtcNow;
                await _db.ApiKeys
                    .Where(k => k.Id == keyId)
                    .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, now));
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not record API key usage");
            }
        }

        private async Task AddUserClaimsAsync(List<Claim> claims)
        {
            if (!Request.Headers.TryGetValue(ApiKeyDefaults.UserTokenHeader, out var tokenValues))
                return;

            var token = tokenValues.ToString().Trim();
            if (string.IsNullOrEmpty(token))
                return;

            var user = await _cache.GetOrCreateAsync($"usertoken:{ApiKeyDefaults.Hash(token)}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = Options.CacheDuration;
                return await _db.UserAccounts
                    .AsNoTracking()
                    .Where(u => u.JWToken == token && u.isLoggedIn == 1)
                    .Select(u => new { u.Id, u.UserType })
                    .FirstOrDefaultAsync();
            });

            if (user?.Id == null)
                return;

            claims.Add(new Claim(ApiKeyDefaults.UserIdClaim, user.Id.Value.ToString()));
            claims.Add(new Claim(ApiKeyDefaults.UserTypeClaim, (user.UserType ?? 0).ToString()));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";
            await Response.WriteAsync(
                "{\"message\":\"Missing or invalid API key. Send it as 'X-Api-Key: <key>' or 'Authorization: Bearer <key>'.\"}");
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.ContentType = "application/json";
            await Response.WriteAsync("{\"message\":\"This API key is not allowed to perform this action.\"}");
        }
    }
}

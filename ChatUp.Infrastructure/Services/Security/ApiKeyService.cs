using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Common.Security;
using ChatUp.Domain.Entities;
using ChatUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatUp.Infrastructure.Services.Security
{
    public sealed record ApiKeyInfo(
        int Id,
        string Name,
        string Role,
        string Preview,
        bool IsActive,
        bool IsBuiltIn,
        bool LabelShowsKey,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? LastUsedAt,
        DateTime? RotatedAt);

    /// <summary>A key together with its raw value — returned only on create, rotate and reveal.</summary>
    public sealed record ApiKeySecret(ApiKeyInfo Key, string Value);

    /// <summary>Creates, rotates, reveals and revokes API keys, keeping the auth cache in sync.</summary>
    public class ApiKeyService
    {
        private readonly ChatDBContext _db;
        private readonly ICryptography _crypto;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(ChatDBContext db, ICryptography crypto, IMemoryCache cache,
            IConfiguration configuration, ILogger<ApiKeyService> logger)
        {
            _db = db;
            _crypto = crypto;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<ApiKeyInfo>> ListAsync(bool includeRevoked)
        {
            var keys = await _db.ApiKeys
                .AsNoTracking()
                .Where(k => includeRevoked || k.IsActive)
                .OrderByDescending(k => k.IsActive)
                .ThenBy(k => k.Id)
                .ToListAsync();

            return keys.Select(ToInfo).ToList();
        }

        public async Task<ApiKeyInfo?> GetAsync(int id)
        {
            var key = await _db.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
            return key == null ? null : ToInfo(key);
        }

        public async Task<ApiKeySecret> CreateAsync(string name, ApiKeyRole role, int? expiresInDays, int? createdBy)
        {
            var value = ApiKeyDefaults.Generate();
            var key = new ApiKey
            {
                Name = name.Trim(),
                Role = role,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresInDays is > 0 ? DateTime.UtcNow.AddDays(expiresInDays.Value) : null,
                CreatedByUserId = createdBy
            };
            SetValue(key, value);

            _db.ApiKeys.Add(key);
            await _db.SaveChangesAsync();
            return new ApiKeySecret(ToInfo(key), value);
        }

        public async Task<ApiKeySecret?> RevealAsync(int id)
        {
            var key = await _db.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id && k.IsActive);
            if (key == null)
                return null;

            return new ApiKeySecret(ToInfo(key), Decrypt(key));
        }

        public async Task<ApiKeyInfo?> UpdateAsync(int id, string? name, int? expiresInDays, bool clearExpiry)
        {
            var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.IsActive);
            if (key == null)
                return null;

            if (!string.IsNullOrWhiteSpace(name))
                key.Name = name.Trim();
            if (clearExpiry)
                key.ExpiresAt = null;
            else if (expiresInDays is > 0)
                key.ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays.Value);

            await _db.SaveChangesAsync();
            Evict(key.KeyHash);
            return ToInfo(key);
        }

        /// <summary>Replaces the key's value; the old value stops working immediately.</summary>
        public async Task<ApiKeySecret?> RotateAsync(int id)
        {
            var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.IsActive);
            if (key == null)
                return null;

            var oldHash = key.KeyHash;
            var value = ApiKeyDefaults.Generate();
            SetValue(key, value);
            key.RotatedAt = DateTime.UtcNow;
            key.IsBuiltIn = false; // a rotated key is no longer the publicly known default

            await _db.SaveChangesAsync();
            Evict(oldHash);
            return new ApiKeySecret(ToInfo(key), value);
        }

        public async Task<bool> RevokeAsync(int id, int? revokedBy)
        {
            var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.IsActive);
            if (key == null)
                return false;

            key.IsActive = false;
            key.RevokedAt = DateTime.UtcNow;
            key.RevokedByUserId = revokedBy;

            await _db.SaveChangesAsync();
            Evict(key.KeyHash);
            return true;
        }

        /// <summary>
        /// First start (or an empty table): create the default UI key so the UI can connect.
        /// Its value comes from "ApiKeys:Default" in appconfig.json, or the built-in fallback.
        /// </summary>
        public async Task EnsureDefaultKeyAsync()
        {
            var configured = _configuration[ApiKeyDefaults.DefaultKeyConfigPath];
            string value;

            if (!string.IsNullOrWhiteSpace(configured))
            {
                // A configured default is added whenever it's new — even after the built-in one was
                // seeded — so the UI (which reads the same appconfig.json) never gets locked out.
                // A revoked row keeps its hash, so a revoked default is not brought back.
                value = configured.Trim();
                var hash = ApiKeyDefaults.Hash(value);
                if (await _db.ApiKeys.AnyAsync(k => k.KeyHash == hash))
                    return;
            }
            else
            {
                // Built-in fallback: only for a brand-new install
                if (await _db.ApiKeys.AnyAsync())
                    return;
                value = ApiKeyDefaults.BuiltInDefaultKey;
            }

            var key = new ApiKey
            {
                Name = "ChatUp UI (default)",
                Role = ApiKeyRole.ApiUser,
                CreatedAt = DateTime.UtcNow,
                IsBuiltIn = value == ApiKeyDefaults.BuiltInDefaultKey
            };
            SetValue(key, value);

            _db.ApiKeys.Add(key);
            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Seeded the default API key for the ChatUp UI ({Preview}). Create a dedicated key in Settings → API keys and connect the UI to it.",
                key.KeyPreview);
        }

        private void SetValue(ApiKey key, string value)
        {
            key.KeyHash = ApiKeyDefaults.Hash(value);
            key.KeyEncrypted = _crypto.Encrypt(value);
            key.KeyPreview = ApiKeyDefaults.Mask(value);
        }

        private string Decrypt(ApiKey key)
        {
            try
            {
                return _crypto.Decrypt(key.KeyEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not decrypt API key {Id}", key.Id);
                return string.Empty;
            }
        }

        private void Evict(string hash) =>
            _cache.Remove(ApiKeyAuthenticationHandler.CacheKeyFor(hash));

        private ApiKeyInfo ToInfo(ApiKey k)
        {
            // Flag keys whose label is the key itself (e.g. named after their own value)
            var labelShowsKey = false;
            if (!string.IsNullOrWhiteSpace(k.Name))
                labelShowsKey = ApiKeyDefaults.Hash(k.Name) == k.KeyHash;

            return new ApiKeyInfo(k.Id, k.Name, k.Role.ToString(), k.KeyPreview, k.IsActive, k.IsBuiltIn,
                labelShowsKey, k.CreatedAt, k.ExpiresAt, k.LastUsedAt, k.RotatedAt);
        }
    }
}

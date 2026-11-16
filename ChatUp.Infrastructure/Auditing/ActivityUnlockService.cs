using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.AdminActivity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ChatUp.Infrastructure.Auditing
{
    /// <summary>
    /// "Protected details" (IP address, device, route, session and request ids) stay hidden in the audit log until the
    /// signed-in administrator re-enters their own password. That returns a short-lived token bound to them.
    /// </summary>
    public sealed class ActivityUnlockService : IActivityUnlockService
    {
        private const string Purpose = "ChatUp.AuditLog.ProtectedDetails.v1";
        private const int Admin = 1;
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);

        private readonly IChatDBContext _db;
        private readonly ICryptography _crypto;
        private readonly IDataProtectionProvider _dataProtection;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public ActivityUnlockService(IChatDBContext db, ICryptography crypto, IDataProtectionProvider dataProtection, IMemoryCache cache, IConfiguration config)
        {
            _db = db;
            _crypto = crypto;
            _dataProtection = dataProtection;
            _cache = cache;
            _config = config;
        }

        private ITimeLimitedDataProtector Protector => _dataProtection.CreateProtector(Purpose).ToTimeLimitedDataProtector();

        public async Task<ActivityUnlockResultDto> UnlockAsync(int? userId, string password, CancellationToken ct)
        {
            if (userId is not > 0)
                return Fail("Please sign in again.");

            if (!await CanUnlockAsync(userId, ct))
                return Fail("Only an administrator can unlock the protected details.");

            // Wrong passwords are throttled here, so a few typos never lock the account itself
            var failureKey = $"audit-unlock-failures:{userId}";
            if (_cache.TryGetValue(failureKey, out int failures) && failures >= MaxFailedAttempts)
                return Fail($"Too many failed attempts. Try again in {FailureWindow.TotalMinutes:0} minutes.");

            var stored = await _db.UserAccounts.AsNoTracking()
                .Where(u => u.Id == userId && u.IsDeleted == 0)
                .Select(u => u.Password)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(password) || !PasswordMatches(password, stored))
            {
                _cache.Set(failureKey, failures + 1, FailureWindow);
                return Fail("Incorrect password.");
            }

            _cache.Remove(failureKey);

            var minutes = Math.Clamp(_config.GetValue("ActivityLog:UnlockMinutes", 15), 1, 240);
            var expires = DateTime.UtcNow.AddMinutes(minutes);

            return new ActivityUnlockResultDto
            {
                IsSuccess = true,
                Message = $"Protected details unlocked for {minutes} minutes.",
                Token = Protector.Protect(userId.Value.ToString(), new DateTimeOffset(expires)),
                ExpiresAtUtc = expires
            };
        }

        public bool IsUnlocked(string? token, int? userId)
        {
            if (string.IsNullOrEmpty(token) || userId is not > 0)
                return false;

            try
            {
                // Throws when expired, tampered with or issued by another server key
                return Protector.Unprotect(token) == userId.Value.ToString();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CanUnlockAsync(int? userId, CancellationToken ct) =>
            userId is > 0 && await _db.UserAccounts.AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.IsDeleted == 0 && u.UserType == Admin, ct);

        // Same rules as sign-in: BCrypt hashes, or the older reversible encryption
        private bool PasswordMatches(string password, string stored)
        {
            try
            {
                if (stored.StartsWith("$2"))
                    return BCrypt.Net.BCrypt.Verify(password, stored);

                return _crypto.Decrypt(stored) == password;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static ActivityUnlockResultDto Fail(string message) => new() { IsSuccess = false, Message = message };
    }
}

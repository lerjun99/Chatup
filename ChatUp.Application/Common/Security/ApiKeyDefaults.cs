using System.Security.Cryptography;
using System.Text;

namespace ChatUp.Application.Common.Security
{
    /// <summary>
    /// API key conventions shared by the API and the UI (both reference this assembly).
    /// </summary>
    public static class ApiKeyDefaults
    {
        public const string Scheme = "ApiKey";

        /// <summary>Policy: any valid API key (applied to every controller and hub).</summary>
        public const string ApiAccessPolicy = "ApiKey";

        /// <summary>Policy: an Admin key, or any key plus a signed-in administrator's session token.</summary>
        public const string AdminPolicy = "ApiKeyAdmin";

        public const string HeaderName = "X-Api-Key";

        /// <summary>The signed-in user's session token (UserAccount.JWToken), sent by the UI for admin actions.</summary>
        public const string UserTokenHeader = "X-User-Token";

        public const string KeyPrefix = "chk_";

        // Claims added by the API key handler
        public const string KeyIdClaim = "apikey_id";
        public const string KeyNameClaim = "apikey_name";
        public const string UserIdClaim = "user_id";
        public const string UserTypeClaim = "user_type";

        /// <summary>Config path (C:\app\chatup\appconfig.json) of the shared default UI key.</summary>
        public const string DefaultKeyConfigPath = "ApiKeys:Default";

        /// <summary>
        /// Used only when no key is configured anywhere, so a fresh install still works.
        /// The UI warns while it is in use — set "ApiKeys:Default" or connect a real key.
        /// </summary>
        public const string BuiltInDefaultKey = "chk_builtin_default_change_me";

        public static string Hash(string rawKey)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>A new random key, e.g. "chk_3qVb…" (32 random bytes, URL-safe).</summary>
        public static string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return KeyPrefix + token;
        }

        /// <summary>"chk_••••••••x9Q2" — enough to recognise a key, never enough to use it.</summary>
        public static string Mask(string rawKey)
        {
            var key = rawKey.Trim();
            if (key.Length <= 8)
                return new string('•', Math.Max(4, key.Length));

            return $"{key[..4]}••••••••{key[^4..]}";
        }
    }
}

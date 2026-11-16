using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Web;

namespace ChatUp.Services
{
    public class LinkService
    {
        private readonly IDataProtector _protector;
        private readonly IDataProtectionProvider _provider;

        public LinkService(IDataProtectionProvider provider)
        {
            _provider = provider;
            _protector = provider.CreateProtector("ChatLinkProtector.v1");
        }

        /// <summary>
        /// A signed value that only works for <paramref name="purpose"/> and stops working after <paramref name="lifetime"/>
        /// (e.g. the password-reset link handed out after a one-time code is verified). URL-safe.
        /// </summary>
        public string ProtectTimed(string purpose, string value, TimeSpan lifetime)
        {
            var timed = _provider.CreateProtector(purpose).ToTimeLimitedDataProtector();
            return Uri.EscapeDataString(timed.Protect(value, lifetime));
        }

        /// <summary>The value from <see cref="ProtectTimed"/>, or null when it is forged, for another purpose, or expired.</summary>
        public string? UnprotectTimed(string purpose, string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                var timed = _provider.CreateProtector(purpose).ToTimeLimitedDataProtector();
                return timed.Unprotect(Uri.UnescapeDataString(token));
            }
            catch
            {
                return null;
            }
        }

        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            var protectedValue = _protector.Protect(plainText);
            return Uri.EscapeDataString(protectedValue);
        }

        public string? Unprotect(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                var decoded = Uri.UnescapeDataString(token);
                return _protector.Unprotect(decoded);
            }
            catch
            {
                return null;
            }
        }
    }
}
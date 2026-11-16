using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace ChatUp.Application.Auditing
{
    public enum FieldSensitivity { None, Masked, Hidden }

    /// <summary>Shared rules for turning column names and values into what the audit log shows.</summary>
    public static partial class ChangeFormatter
    {
        public const int MaxValueLength = 500;
        public const string MaskedValue = "••••••";

        // Bookkeeping columns: who/when is already on the activity row itself
        private static readonly HashSet<string> AuditColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "DeletedBy", "DateCreated", "DateUpdated", "DateDeleted",
            "ConcurrencyStamp", "SecurityStamp", "RowVersion", "Timestamp", "LastUsedAt"
        };

        // Values never written to the log, only the fact that they changed
        private static readonly string[] MaskedFragments = { "Password" };

        // Secrets, internal values and file payloads left out entirely
        private static readonly string[] HiddenFragments =
        {
            "Token", "Secret", "Otp", "ApiKey", "KeyHash", "KeyEncrypted", "Salt", "Hash", "Base64", "Thumbnail"
        };

        public static FieldSensitivity GetSensitivity(string propertyName)
        {
            if (AuditColumns.Contains(propertyName) || propertyName.StartsWith("Normalized", StringComparison.OrdinalIgnoreCase))
                return FieldSensitivity.Hidden;

            if (MaskedFragments.Any(f => propertyName.Contains(f, StringComparison.OrdinalIgnoreCase)))
                return FieldSensitivity.Masked;

            if (HiddenFragments.Any(f => propertyName.Contains(f, StringComparison.OrdinalIgnoreCase)))
                return FieldSensitivity.Hidden;

            return FieldSensitivity.None;
        }

        /// <summary>"IssueTitle" → "Issue Title", "ClientId" → "Client", "IsCase" → "Case".</summary>
        public static string Humanize(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var value = name.Trim();

            if (value.Length > 3 && value.EndsWith("Id", StringComparison.Ordinal) && char.IsLower(value[^3]))
                value = value[..^2];
            else if (value.Length > 3 && value.EndsWith("ID", StringComparison.Ordinal))
                value = value[..^2];

            if (value.Length > 2 && value.StartsWith("Is", StringComparison.Ordinal) && char.IsUpper(value[2]))
                value = value[2..];

            value = value.Replace('_', ' ');
            value = SplitWordsRegex().Replace(value, " ");
            value = MultiSpaceRegex().Replace(value, " ").Trim();

            return value.Length == 0 ? name : char.ToUpperInvariant(value[0]) + value[1..];
        }

        public static string? Format(object? value)
        {
            var text = value switch
            {
                null => null,
                string s => CleanText(s),
                bool b => b ? "Yes" : "No",
                DateTime d => d.TimeOfDay == TimeSpan.Zero
                    ? d.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture)
                    : d.ToString("MMM dd, yyyy hh:mm tt", CultureInfo.InvariantCulture),
                DateTimeOffset d => d.ToString("MMM dd, yyyy hh:mm tt zzz", CultureInfo.InvariantCulture),
                DateOnly d => d.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                TimeSpan t => t.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                TimeOnly t => t.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                byte[] bytes => $"(binary, {bytes.Length} bytes)",
                Enum e => Humanize(e.ToString()),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

            if (string.IsNullOrEmpty(text))
                return null;

            return text.Length > MaxValueLength ? text[..MaxValueLength] + "…" : text;
        }

        /// <summary>Null and empty string count as the same value; so do texts that only differ in surrounding whitespace.</summary>
        public static bool AreEqual(object? a, object? b)
        {
            if (a is string sa && b is string sb)
                return string.Equals(sa.Trim(), sb.Trim(), StringComparison.Ordinal);

            if ((a is null || a is string { Length: 0 }) && (b is null || b is string { Length: 0 }))
                return true;

            return Equals(a, b);
        }

        public static string Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..(max - 1)] + "…";

        /// <summary>The message without its "Changes:" block.</summary>
        public static string StripChangeBlock(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            var index = message.IndexOf("Changes:", StringComparison.Ordinal);
            return (index < 0 ? message : message[..index]).Trim();
        }

        /// <summary>Rich text is stored as HTML: show the words, not the markup.</summary>
        private static string CleanText(string value)
        {
            if (value.IndexOf('<') < 0)
                return value.Trim();

            var text = HtmlTagRegex().Replace(value, " ");
            text = WebUtility.HtmlDecode(text);
            return MultiSpaceRegex().Replace(text, " ").Trim();
        }

        [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
        private static partial Regex SplitWordsRegex();

        [GeneratedRegex(@"\s{2,}")]
        private static partial Regex MultiSpaceRegex();

        [GeneratedRegex(@"<[^>]*>")]
        private static partial Regex HtmlTagRegex();
    }

    /// <summary>
    /// API routes ("POST /TicketMessage/...") are protected details, like the IP address: they reveal the API's
    /// endpoints. They're kept out of the summary/error shown until an administrator unlocks the protected details.
    /// </summary>
    public static partial class ActivityRedaction
    {
        public const string Placeholder = "[protected route]";

        [GeneratedRegex(@"\b(?:GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\s+/[^\s)\]""',;]*", RegexOptions.IgnoreCase)]
        private static partial Regex RoutePattern();

        public static string? HideRoutes(string? text) =>
            string.IsNullOrEmpty(text) ? text : RoutePattern().Replace(text, Placeholder);
    }
}

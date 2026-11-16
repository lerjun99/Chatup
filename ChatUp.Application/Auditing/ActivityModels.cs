using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatUp.Application.Auditing
{
    /// <summary>One changed column.</summary>
    public sealed class FieldChange
    {
        public string Field { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Old { get; set; }
        public string? New { get; set; }
    }

    /// <summary>One inserted / updated / deleted row and its changed columns.</summary>
    public sealed class EntityChange
    {
        public string Entity { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public List<FieldChange> Fields { get; set; } = new();
    }

    /// <summary>What a handler reported through <see cref="AuditTrail"/> (summary, record id, curated changes).</summary>
    public sealed record AuditNote(string Module, string? RefId, string Message, IReadOnlyList<FieldChange> Changes);

    public static class ActivityJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string? Serialize(IReadOnlyCollection<EntityChange> changes) =>
            changes.Count == 0 ? null : JsonSerializer.Serialize(changes, Options);

        public static List<EntityChange> Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();

            try { return JsonSerializer.Deserialize<List<EntityChange>>(json, Options) ?? new(); }
            catch (JsonException) { return new(); }
        }
    }

    public static class ActivityCategories
    {
        public const string Auth = "Auth";
        public const string Navigation = "Navigation";
        public const string Data = "Data";
        public const string Security = "Security";
        public const string System = "System";

        public static readonly string[] All = { Auth, Navigation, Data, Security, System };
    }

    public static class ActivityActions
    {
        public const string Login = "Login";
        public const string LoginFailed = "LoginFailed";
        public const string Logout = "Logout";
        public const string PageView = "PageView";
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Restore = "Restore";
        public const string AccessDenied = "AccessDenied";
        public const string ProtectedDetailsUnlocked = "ProtectedDetailsUnlocked";
        public const string ProtectedDetailsUnlockFailed = "ProtectedDetailsUnlockFailed";
        public const string Error = "Error";
    }

    public static class ActivitySources
    {
        /// <summary>The ChatUp web app (the call carried the signed-in user's session).</summary>
        public const string Web = "Web";
        /// <summary>Another system calling the API with its own key.</summary>
        public const string Api = "Api";
        public const string System = "System";
    }

    /// <summary>Request headers the ChatUp UI adds so the API can attribute activity to the real browser.</summary>
    public static class ActivityHeaders
    {
        public const string ClientIp = "X-Activity-Client-Ip";
        public const string UserAgent = "X-Activity-User-Agent";
        public const string SessionId = "X-Activity-Session";
        public const string CorrelationId = "X-Correlation-Id";
    }
}

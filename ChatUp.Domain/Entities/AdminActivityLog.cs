using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatUp.Domain.Entities
{
    /// <summary>
    /// Audit log (same model as the AFL-CMS "Admin Activity"): one row per user activity — a sign-in, a page visit,
    /// a create/update/delete with its exact field changes, a refused or failed request.
    /// (The older <see cref="ActivityLog"/> table is the ticket activity feed on the dashboard and stays as it is.)
    /// </summary>
    public class AdminActivityLog
    {
        public long Id { get; set; }

        [Column(TypeName = "datetime2(3)")]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>Same for every row written by one HTTP request.</summary>
        public Guid CorrelationId { get; set; }

        /// <summary>The browser session (one per sign-in tab), linking Login → … → Logout.</summary>
        [Column(TypeName = "varchar(64)")]
        public string? SessionId { get; set; }

        public int? UserId { get; set; }

        [Column(TypeName = "nvarchar(256)")]
        public string? UserName { get; set; }

        /// <summary>Auth | Navigation | Data | Security | System</summary>
        [Column(TypeName = "varchar(20)")]
        public string Category { get; set; } = string.Empty;

        /// <summary>Login, LoginFailed, Logout, PageView, Create, Update, Delete, AccessDenied, Error…</summary>
        [Column(TypeName = "varchar(50)")]
        public string Action { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(100)")]
        public string Module { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(100)")]
        public string? EntityName { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string? EntityId { get; set; }

        /// <summary>Human readable one-liner shown in the list.</summary>
        [Column(TypeName = "nvarchar(500)")]
        public string Summary { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Details { get; set; }

        /// <summary>JSON: [{ entity, entityId, action, fields: [{ field, label, old, new }] }].</summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Changes { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? Route { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? HttpMethod { get; set; }

        public int? StatusCode { get; set; }

        public int? DurationMs { get; set; }

        public bool IsSuccess { get; set; } = true;

        [Column(TypeName = "nvarchar(1000)")]
        public string? ErrorMessage { get; set; }

        [Column(TypeName = "varchar(45)")]
        public string? IpAddress { get; set; }

        [Column(TypeName = "nvarchar(400)")]
        public string? UserAgent { get; set; }

        /// <summary>Web | Api | System</summary>
        [Column(TypeName = "varchar(20)")]
        public string? Source { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatUp.Domain.Entities
{
    public enum ApiKeyRole
    {
        Admin = 1,   // every endpoint, including API key management
        ApiUser = 2  // every endpoint except API key management
    }

    /// <summary>
    /// An API key used by the ChatUp UI and integrations to call the API.
    /// The raw value is never stored: <see cref="KeyHash"/> is used for lookups and
    /// <see cref="KeyEncrypted"/> lets an administrator reveal it again.
    /// </summary>
    public class ApiKey
    {
        public int Id { get; set; }

        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>SHA-256 (hex) of the raw key — unique, indexed lookup.</summary>
        [MaxLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>Reversibly encrypted raw key (for "Reveal").</summary>
        [Column(TypeName = "nvarchar(max)")]
        public string KeyEncrypted { get; set; } = string.Empty;

        /// <summary>Masked form for display, e.g. "chk_••••••••x9Q2".</summary>
        [MaxLength(40)]
        public string KeyPreview { get; set; } = string.Empty;

        public ApiKeyRole Role { get; set; } = ApiKeyRole.ApiUser;

        public bool IsActive { get; set; } = true;
        public bool IsBuiltIn { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? RotatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public int? CreatedByUserId { get; set; }
        public int? RevokedByUserId { get; set; }
    }
}

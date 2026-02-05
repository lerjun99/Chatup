using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class LoginHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }

        // ✅ NEW: Client IP Address
        [MaxLength(45)] // Supports IPv4 + IPv6
        public string? IpAddress { get; set; }

        // ✅ OPTIONAL: User Agent (Browser / Device)
        [MaxLength(512)]
        public string? UserAgent { get; set; }

        // ❌ Not persisted
        [NotMapped]
        public TimeSpan? Duration => LogoutTime.HasValue
            ? LogoutTime.Value - LoginTime
            : null;
    }
}

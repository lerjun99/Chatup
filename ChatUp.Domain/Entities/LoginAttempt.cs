using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class LoginAttempt
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("UserAccount")]
        public int UserId { get; set; }  // FK to UserAccount table
        [Column(TypeName = "varchar(150)")]
        public string? IPAddress { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string? Location { get; set; }

        public int? AttemptCount { get; set; } = 1;

        public bool IsSuccessful { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string? Remarks { get; set; }

        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        // Optional navigation property
        public virtual UserAccount? UserAccount { get; set; }
    }
}

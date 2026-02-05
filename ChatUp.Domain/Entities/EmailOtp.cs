using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class EmailOtp
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string OtpHash { get; set; } = null!;
        public DateTime Expiry { get; set; }
        public bool IsVerified { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

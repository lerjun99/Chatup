using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.DTOs
{
    public class LoginInfoDto
    {
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public double? DurationMinutes { get; set; }
    }
}

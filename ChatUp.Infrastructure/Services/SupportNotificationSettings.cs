using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class SupportNotificationSettings
    {
        public string Email { get; set; } = string.Empty;

        public int ReminderMinutes { get; set; } = 1;
    }
}

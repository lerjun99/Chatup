using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class BusinessCalendarConfig
    {
        public int Id { get; set; }
        public string Region { get; set; }

        public TimeSpan WorkStart { get; set; }
        public TimeSpan WorkEnd { get; set; }

        public string WorkingDaysJson { get; set; } // ["Monday","Tuesday"]
    }
}

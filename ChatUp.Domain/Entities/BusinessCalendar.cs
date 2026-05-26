using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class BusinessCalendar
    {
        public int Id { get; set; } // ✅ PRIMARY KEY
        public string Region { get; set; }
        public TimeSpan WorkStart { get; set; }
        public TimeSpan WorkEnd { get; set; }

        // STORE AS JSON STRING (NOT COLLECTION)
        public List<DayOfWeek> WorkingDays { get; set; } = new();
        public List<DateTime> Holidays { get; set; } = new();
    }
}

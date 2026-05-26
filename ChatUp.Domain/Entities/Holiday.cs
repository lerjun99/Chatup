using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class Holiday
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }   // Date only
        public string Name { get; set; }
        public string Region { get; set; }   // PH, US, etc.
        public bool IsRecurring { get; set; } // optional
    }
}

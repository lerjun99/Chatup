using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public string TeamName { get; set; }
        public int? SuperVisorId { get; set; }
        public bool DeleteFlag { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? ClientId { get; set; }

        // 🔗 Navigation properties
        public ICollection<Project> Projects { get; set; }
    }
}

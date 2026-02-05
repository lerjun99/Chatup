using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{

    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Foreign Key to Client
        public int ClientId { get; set; }
        public Client Client { get; set; }

        // Foreign Key to Team
        public int TeamId { get; set; }
        public Team Team { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Title { get; set; }

        [Column(TypeName = "varchar(max)")]
        public string Description { get; set; }

        public bool DeleteFlag { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }

        // ✅ make nullable
        public int? UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }

        // ✅ make nullable
        public bool? DeletedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int? ContractId { get; set; }
        public Contract Contract { get; set; }

        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class Contract
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        [Column(TypeName = "varchar(200)")]
        public string Title { get; set; }


        [Column(TypeName = "varchar(max)")]
        public string Description { get; set; }

        // ✅ Add these properties (to match DTO and logic)
        [Column(TypeName = "datetime")]
        public DateTime? StartDate { get; set; }
        // Important for scheduling
        [Column(TypeName = "datetime")]
        public DateTime? ExpirationDate { get; set; }


        public bool IsTerminated { get; set; }
        public bool IsActive { get; set; } = true;


        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }

        public int? ClientId { get; set; }
        public Client? Client { get; set; }
        // Navigation
        public ICollection<UserContract> UserContracts { get; set; } = new List<UserContract>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}

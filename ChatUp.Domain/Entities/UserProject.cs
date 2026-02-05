using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class UserProject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int? UserType { get; set; }
        [NotMapped]
        public Team Team => Project?.Team;

        // Optional: convenience for TeamName directly
        [NotMapped]
        public string TeamName => Project?.Team?.TeamName;
    }
}

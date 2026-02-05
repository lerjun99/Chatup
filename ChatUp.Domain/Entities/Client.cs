using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public partial class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string ClientName { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Location { get; set; }

        [Column(TypeName = "varchar(150)")]
        public string? EmailAddress { get; set; }

        [Column(TypeName = "varchar(150)")]
        public string? Industry { get; set; }

        [Column(TypeName = "varchar(150)")]
        public string? Email { get; set; }

        [Column(TypeName = "varchar(150)")]
        public string? TelephoneNumber { get; set; }

        [Column(TypeName = "varchar(150)")]
        public string? PhotoUrl { get; set; }
        public int? IsActive { get; set; }
        public int? IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public int? DeletedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? Status { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateUpdated { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateDeleted { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateCreated { get; set; }

    }
}

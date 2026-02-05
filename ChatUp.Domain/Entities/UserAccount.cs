using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public partial class UserAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Required]
        public string Username { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        [Required]
        public string Password { get; set; } = string.Empty;

        [Column(TypeName = "varchar(150)")]
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(150)")]
        public string? EmailAddress { get; set; }

        public int? Role { get; set; }
        public int? UserType { get; set; }
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

        public List<UploadedFile>? Uploads { get; set; } = new();

        public int? isLoggedIn { get; set; }

        // ✅ Determines whether the user belongs to a client
        public bool IsClient { get; set; } = false;
        public bool IsFirstLogIn { get; set; } = false;

        // ✅ Optional Client reference (only used if IsClient == true)
        public int? ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? RememberToken { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? JWToken { get; set; }

        // Navigation properties
        public ICollection<UserContract> UserContracts { get; set; } = new List<UserContract>();
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}

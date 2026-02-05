using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class UploadedFile
    {

        [Key]
        public int? Id { get; set; }

        public string? Name { get; set; }

        public int? Size { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Base64Content { get; set; }

        public int? UserAccountId { get; set; }

        [ForeignKey(nameof(UserAccountId))]
        public UserAccount? UserAccount { get; set; }

        [Column(TypeName = "varchar(300)")]
        public string ContentType { get; set; }

        [Column(TypeName = "varchar(300)")]
        public string FileType { get; set; }

    }
}

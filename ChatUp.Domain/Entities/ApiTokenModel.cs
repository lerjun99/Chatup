using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class ApiTokenModel
    {
        public int Id { get; set; }
        [Column(TypeName = "varchar(MAX)")]
        public string ApiToken { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string Role { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string Name { get; set; }
        public int Status { get; set; }
    }
}

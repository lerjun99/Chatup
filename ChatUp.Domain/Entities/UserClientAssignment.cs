using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class UserClientAssignment
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ClientId { get; set; }
        public int? UserType { get; set; }

        public UserAccount? User { get; set; }
        public Client? Client { get; set; }
    }
}

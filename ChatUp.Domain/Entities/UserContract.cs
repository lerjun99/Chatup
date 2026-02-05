using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class UserContract
    {
        public int UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }

        public int ContractId { get; set; }
        public Contract Contract { get; set; }

        // Optional: Role of user in this contract
        public int? UserType { get; set; }
    }
}

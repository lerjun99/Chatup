using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class ChatUser
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";  
        public int UserId { get; set; } 
        public string DisplayName { get; set; } = ""; 
        public string AvatarUrl { get; set; } = "";   

        public ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
        public ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
    }
}

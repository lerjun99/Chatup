using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
  
        public class ChatUser
        {
            public int Id { get; set; }
            public string UserName { get; set; } = "";   // e.g., "Alice"
            public string DisplayName { get; set; } = ""; // e.g., "Alice Johnson"
            public string AvatarUrl { get; set; } = "";   // optional profile pic

            public ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
            public ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
        }
        public class ChatMessage
        {
            public int Id { get; set; }

            public int SenderId { get; set; }
            public ChatUser Sender { get; set; } = null!;

            public int ReceiverId { get; set; }
            public ChatUser Receiver { get; set; } = null!;

            public string Text { get; set; } = "";
            public DateTime Timestamp { get; set; }
        }
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

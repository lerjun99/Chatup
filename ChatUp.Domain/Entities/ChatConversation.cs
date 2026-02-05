using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class ChatConversation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // optional for group chats
        public bool IsGroup { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsLocked { get; set; } = false; // lock conversation so participants can't send messages

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int SenderId { get; set; }    // references ChatUser.UserId
        [ForeignKey(nameof(SenderId))]
        public ChatUser Sender { get; set; } = null!;

        public int ReceiverId { get; set; }  // references ChatUser.UserId
        [ForeignKey(nameof(ReceiverId))]
        public ChatUser Receiver { get; set; } = null!;

        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int ConversationId { get; set; }        // FK to conversation
        public ChatConversation Conversation { get; set; } // Navigation
        public ICollection<UploadedFile> Attachments { get; set; } = new List<UploadedFile>();
    }
}

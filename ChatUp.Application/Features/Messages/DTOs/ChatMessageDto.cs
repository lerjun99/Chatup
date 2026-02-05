using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.DTOs
{
    public class ChatMessageDto
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.DTOs
{
    public class CloseConversationDto
    {
        public int ConversationId { get; set; }
        public int RequestedBy { get; set; }
    }
}

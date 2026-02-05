using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.Commands
{
    public class CloseConversationCommand : IRequest<Unit>
    {
        public int ConversationId { get; set; }
        public int RequestedBy { get; set; }
    }
}

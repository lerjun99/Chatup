using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Messages.Commands
{
    public class CreateChatCommand : IRequest<int>
    {
        public string? Name { get; set; } // Optional for single chats
        public bool IsGroup { get; set; } = false;
        public List<int> UserIds { get; set; } = new(); // Users to invite (must include creator)
        public int CreatedBy { get; set; }
    }
}

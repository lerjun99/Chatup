using ChatUp.Application.Features.TicketMessage.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Commands
{
    public class AddTicketMessageCommand : IRequest<MessageDto>
    {
        public int TicketId { get; set; }
        public int SenderId { get; set; }
        public bool IsUser { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<TicketUploadDto>? Attachments { get; set; } // optional attachments
    }
}

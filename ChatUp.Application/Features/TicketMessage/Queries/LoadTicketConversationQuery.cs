using ChatUp.Application.Features.TicketMessage.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Queries
{
    public class LoadTicketConversationQuery : IRequest<TicketConversationDto>
    {
        public int TicketId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public LoadTicketConversationQuery(int ticketId, int pageNumber = 1, int pageSize = 20)
        {
            TicketId = ticketId;
            PageNumber = pageNumber > 0 ? pageNumber : 1;
            PageSize = pageSize > 0 ? pageSize : 20;
        }
    }
}

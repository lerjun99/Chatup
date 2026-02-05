using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class TicketPagedResponse
    {
        public List<TicketDto> Tickets { get; set; } = new();
        public List<TicketStatusCountDto> StatusCounts { get; set; } = new();
    }
    public class TicketStatusCountDto
    {
        public TicketStatus Status { get; set; }
        public int Count { get; set; }
    }
}

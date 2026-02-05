using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class UpdateTicketTitleDto
    {
        public int TicketId { get; set; }
        public string NewTitle { get; set; }
    }
}

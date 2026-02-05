using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ChatUp.Application.Tickets.Commands.DeleteTicket
{
    public class DeleteTicketCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public DeleteTicketCommand(int id) => Id = id;
    }
}
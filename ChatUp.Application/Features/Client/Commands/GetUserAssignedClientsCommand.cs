using ChatUp.Application.Features.Client.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Client.Commands
{
    public class GetUserAssignedClientsCommand : IRequest<List<AssignedClientDto>>
    {
        public int UserId { get; set; }
        public GetUserAssignedClientsCommand(int userId)
        {
            UserId = userId;
        }
    }
}

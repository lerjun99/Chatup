using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Commands
{
    public class DeleteUserClientAssignmentCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }
}

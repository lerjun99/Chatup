using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Commands
{
    public class UpdateUserClientAssignmentCommand : IRequest<UserClientAssignmentDto>
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ClientId { get; set; }
    }
}

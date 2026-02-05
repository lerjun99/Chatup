using ChatUp.Application.Features.Projects.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Commands
{
    public class CreateUserProjectAssignmentCommand : IRequest<UserProjectAssignmentDto>
    {
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int? UserType { get; set; }

        public CreateUserProjectAssignmentCommand(CreateUserProjectAssignmentDto dto)
        {
            UserId = dto.UserId;
            ProjectId = dto.ProjectId;
            UserType = dto.UserType;
        }
    }
}

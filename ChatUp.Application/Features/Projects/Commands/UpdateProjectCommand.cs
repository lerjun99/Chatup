using ChatUp.Application.Features.Projects.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Commands
{
    public class UpdateProjectCommand : IRequest<ProjectDto>
    {
        public int Id { get; set; }
        public UpdateProjectDto UpdateDto { get; set; }

        public UpdateProjectCommand(int id, UpdateProjectDto updateDto)
        {
            Id = id;
            UpdateDto = updateDto;
        }
    }
}

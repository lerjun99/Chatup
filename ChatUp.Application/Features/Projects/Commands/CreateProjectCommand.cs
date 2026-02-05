using ChatUp.Application.Features.Projects.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Commands
{
    public class CreateProjectCommand : IRequest<ProjectDto>
    {
        public CreateProjectDto CreateDto { get; set; }
        public CreateProjectCommand(CreateProjectDto dto) => CreateDto = dto;
    }
}

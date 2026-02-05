using ChatUp.Application.Features.Projects.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Commands
{
    public class DeleteProjectCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public DeleteProjectDto DeleteDto { get; set; }

        public DeleteProjectCommand(int id, DeleteProjectDto deleteDto)
        {
            Id = id;
            DeleteDto = deleteDto;
        }
    }
}
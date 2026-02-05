using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.Commands;
using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Handlers
{
    public class UpdateUserProjectAssignmentHandler : IRequestHandler<UpdateUserProjectAssignmentCommand, UserProjectAssignmentDto>
    {
        private readonly IChatDBContext _context;

        public UpdateUserProjectAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<UserProjectAssignmentDto> Handle(UpdateUserProjectAssignmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.UserProjects.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
                throw new Exception("Assignment not found");

            entity.UserAccountId = request.UserId;
            entity.ProjectId = request.ProjectId;

            await _context.SaveChangesAsync(cancellationToken);

            return new UserProjectAssignmentDto
            {
                Id = entity.Id,
                UserId = entity.UserAccountId,
                ProjectId = entity.ProjectId
            };
        }
    }
}

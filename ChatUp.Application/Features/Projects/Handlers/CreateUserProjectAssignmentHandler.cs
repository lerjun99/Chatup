using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.Commands;
using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Handlers
{
    public class CreateUserProjectAssignmentHandler : IRequestHandler<CreateUserProjectAssignmentCommand, UserProjectAssignmentDto>
    {
        private readonly IChatDBContext _context;

        public CreateUserProjectAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<UserProjectAssignmentDto> Handle(CreateUserProjectAssignmentCommand request, CancellationToken cancellationToken)
        {
            // ✅ Check if this user is already assigned to the project
            bool alreadyAssigned = await _context.UserProjects
                .AnyAsync(x => x.UserAccountId == request.UserId && x.ProjectId == request.ProjectId, cancellationToken);

            if (alreadyAssigned)
                throw new InvalidOperationException("This user is already assigned to the selected project.");

            // ✅ Create new assignment
            var assignment = new UserProject
            {
                UserAccountId = request.UserId,
                ProjectId = request.ProjectId,
                UserType = request.UserType
            };

            _context.UserProjects.Add(assignment);
            await _context.SaveChangesAsync(cancellationToken);

            return new UserProjectAssignmentDto
            {
                Id = assignment.ProjectId, // or add an Id field to UserProject
                UserId = assignment.UserAccountId,
                ProjectId = assignment.ProjectId,
                UserType = assignment.UserType ?? 0
            };
        }
    }
}

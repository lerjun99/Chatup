using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Handlers
{
    public class CreateUserClientAssignmentHandler : IRequestHandler<CreateUserClientAssignmentCommand, UserClientAssignmentDto>
    {
        private readonly IChatDBContext _context;

        public CreateUserClientAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<UserClientAssignmentDto> Handle(CreateUserClientAssignmentCommand request, CancellationToken cancellationToken)
        {
            // ✅ 1. Check if this user is already assigned to the same client
            bool userAlreadyAssigned = await _context.UserClientAssignments
                .AnyAsync(x => x.UserId == request.UserId && x.ClientId == request.ClientId, cancellationToken);

            if (userAlreadyAssigned)
                throw new InvalidOperationException("This user is already assigned to the selected client.");

            // ✅ 2. Check if the client already has a different user assigned
            var existingAssignment = await _context.UserClientAssignments
                .FirstOrDefaultAsync(x => x.ClientId == request.ClientId, cancellationToken);

            if (existingAssignment != null)
            {
                // 🟡 Case: Client already has a user — update the existing assignment
                existingAssignment.UserId = request.UserId;
                _context.UserClientAssignments.Update(existingAssignment);
                await _context.SaveChangesAsync(cancellationToken);

                return new UserClientAssignmentDto
                {
                    Id = existingAssignment.Id,
                    UserId = existingAssignment.UserId,
                    ClientId = existingAssignment.ClientId
                };
            }

            // ✅ 3. Case: Client has no user assigned — create a new assignment
            var newAssignment = new UserClientAssignment
            {
                UserId = request.UserId,
                ClientId = request.ClientId,
                UserType = request.UserType
            };

            _context.UserClientAssignments.Add(newAssignment);
            await _context.SaveChangesAsync(cancellationToken);

            // ✅ 4. Return DTO
            return new UserClientAssignmentDto
            {
                Id = newAssignment.Id,
                UserId = newAssignment.UserId,
                ClientId = newAssignment.ClientId
            };
        }
    }
}

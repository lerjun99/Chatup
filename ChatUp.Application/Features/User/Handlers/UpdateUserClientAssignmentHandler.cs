using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.User.Handlers
{
    public class UpdateUserClientAssignmentHandler : IRequestHandler<UpdateUserClientAssignmentCommand, UserClientAssignmentDto>
    {
        private readonly IChatDBContext _context;

        public UpdateUserClientAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<UserClientAssignmentDto> Handle(UpdateUserClientAssignmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.UserClientAssignments
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                ?? throw new InvalidOperationException("Assignment not found.");

            // Moving the assignment onto a pair that already exists would create a duplicate
            bool duplicate = await _context.UserClientAssignments
                .AnyAsync(x => x.Id != request.Id && x.UserId == request.UserId && x.ClientId == request.ClientId, cancellationToken);

            if (duplicate)
                throw new InvalidOperationException("This user is already assigned to the selected client.");

            entity.UserId = request.UserId;
            entity.ClientId = request.ClientId;

            await _context.SaveChangesAsync(cancellationToken);

            return new UserClientAssignmentDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ClientId = entity.ClientId,
                UserType = entity.UserType
            };
        }
    }
}

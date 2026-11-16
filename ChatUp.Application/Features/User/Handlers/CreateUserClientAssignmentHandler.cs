using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.User.Handlers
{
    /// <summary>
    /// Adds a support agent to a client. A client can have any number of support agents — every one of them
    /// sees and answers the client's tickets — so an existing assignment is never replaced.
    /// </summary>
    public class CreateUserClientAssignmentHandler : IRequestHandler<CreateUserClientAssignmentCommand, UserClientAssignmentDto>
    {
        private readonly IChatDBContext _context;

        public CreateUserClientAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<UserClientAssignmentDto> Handle(CreateUserClientAssignmentCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId is not > 0 || request.ClientId is not > 0)
                throw new InvalidOperationException("Choose both a support agent and a client.");

            bool alreadyAssigned = await _context.UserClientAssignments
                .AnyAsync(x => x.UserId == request.UserId && x.ClientId == request.ClientId, cancellationToken);

            if (alreadyAssigned)
                throw new InvalidOperationException("This user is already assigned to the selected client.");

            var assignment = new UserClientAssignment
            {
                UserId = request.UserId,
                ClientId = request.ClientId,
                UserType = request.UserType
            };

            _context.UserClientAssignments.Add(assignment);
            await _context.SaveChangesAsync(cancellationToken);

            return new UserClientAssignmentDto
            {
                Id = assignment.Id,
                UserId = assignment.UserId,
                ClientId = assignment.ClientId,
                UserType = assignment.UserType
            };
        }
    }

    /// <summary>Adds several support agents to one client in a single save; agents already on the client are skipped.</summary>
    public class AssignClientSupportsHandler : IRequestHandler<AssignClientSupportsCommand, AssignClientSupportsResult>
    {
        private readonly IChatDBContext _context;

        public AssignClientSupportsHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<AssignClientSupportsResult> Handle(AssignClientSupportsCommand request, CancellationToken cancellationToken)
        {
            var requested = (request.UserIds ?? new()).Where(id => id > 0).Distinct().ToList();

            if (request.ClientId <= 0 || requested.Count == 0)
                throw new InvalidOperationException("Choose a client and at least one support agent.");

            var clientExists = await _context.Client.AnyAsync(c => c.Id == request.ClientId, cancellationToken);
            if (!clientExists)
                throw new InvalidOperationException("The selected client no longer exists.");

            // Only active support accounts can be assigned
            var validSupportIds = await _context.UserAccounts
                .Where(u => u.Id.HasValue && requested.Contains(u.Id.Value) && u.IsDeleted == 0 && u.UserType == 2)
                .Select(u => u.Id!.Value)
                .ToListAsync(cancellationToken);

            var existing = await _context.UserClientAssignments
                .Where(a => a.ClientId == request.ClientId && a.UserId.HasValue && requested.Contains(a.UserId.Value))
                .Select(a => a.UserId!.Value)
                .ToListAsync(cancellationToken);

            var toAdd = validSupportIds.Except(existing).ToList();

            foreach (var userId in toAdd)
            {
                _context.UserClientAssignments.Add(new UserClientAssignment
                {
                    UserId = userId,
                    ClientId = request.ClientId,
                    UserType = 2
                });
            }

            if (toAdd.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new AssignClientSupportsResult
            {
                Added = toAdd.Count,
                AlreadyAssigned = existing.Count,
                Skipped = requested.Count - validSupportIds.Count
            };
        }
    }
}

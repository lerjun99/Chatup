using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var entity = await _context.UserClientAssignments.FindAsync(request.Id);

            if (entity == null)
                throw new Exception("Assignment not found");

            entity.UserId = request.UserId;
            entity.ClientId = request.ClientId;

            await _context.SaveChangesAsync(cancellationToken);

            return new UserClientAssignmentDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ClientId = entity.ClientId
            };
        }
    }
}

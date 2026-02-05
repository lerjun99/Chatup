using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Handlers
{
    public class DeleteUserClientAssignmentHandler : IRequestHandler<DeleteUserClientAssignmentCommand, bool>
    {
        private readonly IChatDBContext _context;

        public DeleteUserClientAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteUserClientAssignmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.UserClientAssignments.FindAsync(request.Id);
            if (entity == null) return false;

            _context.UserClientAssignments.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

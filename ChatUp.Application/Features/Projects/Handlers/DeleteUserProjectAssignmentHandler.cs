using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Projects.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Handlers
{
    public class DeleteUserProjectAssignmentHandler
         : IRequestHandler<DeleteUserProjectAssignmentCommand, bool>
    {
        private readonly IChatDBContext _context;

        public DeleteUserProjectAssignmentHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteUserProjectAssignmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.UserProjects.FindAsync(new object[] { request.Id }, cancellationToken);

            if (entity == null)
                throw new Exception("Assignment not found");

            _context.UserProjects.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

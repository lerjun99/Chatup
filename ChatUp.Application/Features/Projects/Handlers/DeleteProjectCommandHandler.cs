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
    public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Unit>
    {
        private readonly IProjectRepository _repo;

        public DeleteProjectCommandHandler(IProjectRepository repo) => _repo = repo;

        public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (project == null || project.DeleteFlag)
                throw new KeyNotFoundException($"Project with id {request.Id} not found.");

            project.DeleteFlag = true;
            project.DateDeleted = DateTime.UtcNow;
            project.DeletedBy = request.DeleteDto.DeletedBy;

            await _repo.SoftDeleteAsync(project, cancellationToken);
            return Unit.Value;
        }
    }
}

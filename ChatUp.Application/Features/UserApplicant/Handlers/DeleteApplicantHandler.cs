using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class DeleteApplicantHandler : IRequestHandler<DeleteApplicantCommand>
    {
        private readonly IChatDBContext _context;

        public DeleteApplicantHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteApplicantCommand request, CancellationToken cancellationToken)
        {
            var applicant = await _context.Applicants.FindAsync(request.Id);

            if (applicant == null)
                throw new Exception("Applicant not found");

            if (applicant.IsDeleted)
                return; // already deleted (idempotent)

            applicant.IsDeleted = true;
            applicant.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

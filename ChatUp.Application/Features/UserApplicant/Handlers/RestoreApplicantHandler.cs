using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class RestoreApplicantHandler : IRequestHandler<RestoreApplicantCommand>
    {
        private readonly IChatDBContext _context;

        public RestoreApplicantHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task Handle(RestoreApplicantCommand request, CancellationToken cancellationToken)
        {
            var applicant = await _context.Applicants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (applicant == null)
                throw new Exception("Applicant not found");

            if (!applicant.IsDeleted)
                return; // already active (idempotent)

            applicant.IsDeleted = false;
            applicant.DeletedAt = null;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

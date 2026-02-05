using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Contracts.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Handlers
{
    public class DeleteContractHandler : IRequestHandler<DeleteContractCommand, Unit>
    {
        private readonly IChatDBContext _context;

        public DeleteContractHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteContractCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Contracts
                 .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (entity == null)
                throw new Exception("Contract not found");

            // ✅ Soft delete — just update IsActive to false
            entity.IsActive = false;
            entity.DateUpdated = DateTime.UtcNow; // optional audit field

            _context.Contracts.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}

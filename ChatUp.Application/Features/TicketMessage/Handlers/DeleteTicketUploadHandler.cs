using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class DeleteTicketUploadHandler : IRequestHandler<DeleteTicketUploadCommand, bool>
    {
        private readonly IChatDBContext _context;

        public DeleteTicketUploadHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteTicketUploadCommand request, CancellationToken cancellationToken)
        {
            var upload = await _context.TicketUploads
        .FirstOrDefaultAsync(u => u.Id == request.UploadId, cancellationToken);

            if (upload == null)
                return false;

            // ✅ Soft delete
            upload.IsDeleted = true;

     

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

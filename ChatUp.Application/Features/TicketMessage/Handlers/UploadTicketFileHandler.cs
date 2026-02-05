using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class UploadTicketFileHandler : IRequestHandler<UploadTicketFileCommand, int>
    {
        private readonly IChatDBContext _context;

        public UploadTicketFileHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(UploadTicketFileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Optional: validate ticket exists
                var ticketExists = await _context.Tickets.AnyAsync(t => t.Id == request.TicketId, cancellationToken);
                if (!ticketExists) throw new Exception("Ticket not found.");

                // Only validate TicketMessageId if provided
                if (request.TicketMessageId.HasValue)
                {
                    var msgExists = await _context.TicketMessages.AnyAsync(m => m.Id == request.TicketMessageId.Value, cancellationToken);
                    if (!msgExists) throw new Exception("TicketMessageId not found.");
                }
                string? thumbnailBase64 = null;

                // 🔥 Compress images only
                if (request.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    thumbnailBase64 = ImageCompressionHelper.CreateThumbnail(request.Base64Content);
                }

                var upload = new TicketUpload
                {
                    TicketId = request.TicketId,
                    TicketMessageId = request.TicketMessageId, // can be null
                    UploadedById = request.UploadedById,
                    FileName = request.FileName,
                    FileType = request.FileType,
                    Base64Content = request.Base64Content,
                    ThumbnailBase64 = thumbnailBase64,
                    DateUploaded = DateTime.UtcNow
                };

                _context.TicketUploads.Add(upload);
                await _context.SaveChangesAsync(cancellationToken);

                return upload.Id;
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error while creating ticket upload: {dbEx.Message}");
                if (dbEx.InnerException != null)
                    Console.WriteLine("INNER EXCEPTION: " + dbEx.InnerException.Message);

                throw;
            }
        }
    }
}

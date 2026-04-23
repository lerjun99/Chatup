using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.TicketMessage.Handlers;

public sealed class GetTicketUploadContentHandler : IRequestHandler<GetTicketUploadContentQuery, TicketUploadContentDto?>
{
    private readonly IChatDBContext _context;

    public GetTicketUploadContentHandler(IChatDBContext context)
    {
        _context = context;
    }

    public async Task<TicketUploadContentDto?> Handle(GetTicketUploadContentQuery request, CancellationToken cancellationToken)
    {
        return await _context.TicketUploads
            .AsNoTracking()
            .Where(u => u.Id == request.UploadId && !u.IsDeleted)
            .Select(u => new TicketUploadContentDto
            {
                Id = u.Id,
                FileName = u.FileName,
                FileType = u.FileType,
                Base64Content = request.ThumbnailOnly ? string.Empty : u.Base64Content,
                ThumbnailBase64 = request.ThumbnailOnly ? u.ThumbnailBase64 : u.ThumbnailBase64
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

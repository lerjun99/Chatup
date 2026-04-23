using ChatUp.Application.Features.TicketMessage.DTOs;
using MediatR;

namespace ChatUp.Application.Features.TicketMessage.Queries;

public sealed record GetTicketUploadContentQuery(int UploadId, bool ThumbnailOnly) : IRequest<TicketUploadContentDto?>;

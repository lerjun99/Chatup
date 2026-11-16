using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    /// <summary>
    /// Ticket header details. Messages are NOT loaded here — the chat pages them through the conversation endpoint.
    /// (This used to load every message of the ticket plus one user query per message.)
    /// </summary>
    public class GetTicketByIdHandler : IRequestHandler<GetTicketByIdQueries, TicketDto?>
    {
        private readonly IChatDBContext _context;

        public GetTicketByIdHandler(IChatDBContext context) => _context = context;

        public async Task<TicketDto?> Handle(GetTicketByIdQueries request, CancellationToken cancellationToken)
        {
            var ticket = await _context.Tickets
                .AsNoTracking()
                .Where(t => t.Id == request.TicketId)
                .Select(t => new
                {
                    Dto = new TicketDto
                    {
                        Id = t.Id,
                        TicketNo = t.TicketNo,
                        CaseNo = t.CaseNo,
                        IsCase = t.IsCase,
                        IssueTitle = t.IssueTitle,
                        ProjectId = t.ProjectId,
                        ProjectTitle = t.Project != null ? t.Project.Title : string.Empty,
                        DateReceived = t.DateReceived,
                        DueDate = t.DueDate,
                        Priority = t.Priority,
                        IsArchived = t.IsArchived,
                        RequestedById = t.RequestedById ?? 0,
                        RequestedByName = t.RequestedBy != null ? t.RequestedBy.FullName : string.Empty,
                        ClientId = t.ClientId ?? 0,
                        ClientName = t.Client != null ? t.Client.ClientName : string.Empty,
                        ClientAvatar = t.Client != null ? t.Client.PhotoUrl ?? string.Empty : string.Empty,
                        ClientEmail = t.Client != null ? t.Client.EmailAddress ?? string.Empty : string.Empty,
                        SupportedById = t.SupportedById ?? 0,
                        SupportedByName = t.SupportedBy != null ? t.SupportedBy.FullName : string.Empty,
                        LastClientMessageAt = t.LastClientMessageAt,
                        NeedsReply = t.LastClientMessageAt != null &&
                                     (t.LastSupportReplyAt == null || t.LastSupportReplyAt < t.LastClientMessageAt)
                    },
                    t.Status
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ticket == null)
                return null;

            var dto = ticket.Dto;
            dto.Status = (ticket.Status ?? TicketStatus.Open).ToString();

            if (dto.ClientId > 0)
            {
                var clientId = dto.ClientId;
                dto.SupportTeam = await _context.UserClientAssignments
                    .AsNoTracking()
                    .Where(a => a.ClientId == clientId && a.User != null && a.User.IsDeleted == 0 &&
                                a.User.UserType == TicketAudience.Support)
                    .Select(a => new SupportAgentDto
                    {
                        UserId = a.User!.Id ?? 0,
                        FullName = a.User.FullName,
                        IsOnline = a.User.isLoggedIn == 1,
                        Avatar = _context.UploadedFile
                            .Where(f => f.UserAccountId == a.UserId && f.FileType == "ProfileImg")
                            .OrderByDescending(f => f.Id)
                            .Select(f => f.Base64Content)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                dto.SupportTeam = dto.SupportTeam
                    .GroupBy(s => s.UserId)
                    .Select(g => g.First())
                    .OrderBy(s => s.FullName)
                    .ToList();
            }

            return dto;
        }
    }
}

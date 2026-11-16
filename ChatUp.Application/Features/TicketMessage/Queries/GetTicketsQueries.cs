using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;

namespace ChatUp.Application.Features.TicketMessage.Queries
{
    public class GetTicketsQueries : IRequest<TicketPagedResponse>
    {
        public int UserId { get; set; }
        public string? Search { get; set; }
        public TicketStatus? StatusFilter { get; set; }
        public TicketPriority? PriorityFilter { get; set; }
        public bool IncludeMessages { get; set; }
        public bool IncludeArchived { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        /// <summary>Only tickets of this client.</summary>
        public int? ClientId { get; set; }

        /// <summary>Only tickets whose latest client message has no support reply yet (auto replies don't count).</summary>
        public bool NeedsReply { get; set; }

        /// <summary>Only tickets this user is the supporting agent of.</summary>
        public bool AssignedToMe { get; set; }

        /// <summary>recent (default) · waiting · priority · due</summary>
        public string? Sort { get; set; }

        /// <summary>Ticket status histories; the inbox list doesn't need them.</summary>
        public bool IncludeHistories { get; set; } = true;

        public GetTicketsQueries(
            int userId,
            string? search,
            TicketStatus? statusFilter,
            TicketPriority? priorityFilter,
            bool includeMessages,
            int page,
            int pageSize,
            bool includeArchived = false)
        {
            UserId = userId;
            Search = search;
            StatusFilter = statusFilter;
            PriorityFilter = priorityFilter;
            IncludeMessages = includeMessages;
            IncludeArchived = includeArchived;
            Page = page;
            PageSize = pageSize;
        }
    }
}

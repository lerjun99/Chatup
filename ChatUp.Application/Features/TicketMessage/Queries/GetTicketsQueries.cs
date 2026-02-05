using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

using ChatUp.Domain.Entities;

namespace ChatUp.Application.Features.TicketMessage.DTOs
{
    public class TicketPagedResponse
    {
        public List<TicketDto> Tickets { get; set; } = new();

        /// <summary>Per status, over every status (closed included), with the search/priority/client filters applied.</summary>
        public List<TicketStatusCountDto> StatusCounts { get; set; } = new();

        /// <summary>Active tickets (not closed or rejected) waiting on a support reply.</summary>
        public int NeedsReplyCount { get; set; }

        /// <summary>Tickets matching every filter, across all pages.</summary>
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }

    public class TicketStatusCountDto
    {
        public TicketStatus Status { get; set; }
        public int Count { get; set; }
        public int NeedsReplyCount { get; set; }
    }
}

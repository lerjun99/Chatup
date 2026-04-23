using System;
using ChatUp.Application.Features.Activity.DTOs;
using MediatR;

namespace ChatUp.Application.Features.Activity.Queries
{
    public class GetActivityLogsQuery : IRequest<ActivityPagedResponse>
    {
        public int UserId { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 50;
    }
}

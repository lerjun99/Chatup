using System.Collections.Generic;

namespace ChatUp.Application.Features.Activity.DTOs
{
    public class ActivityPagedResponse
    {
        public List<ActivityLogDto> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}

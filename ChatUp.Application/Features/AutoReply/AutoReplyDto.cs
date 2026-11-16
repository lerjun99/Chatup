namespace ChatUp.Application.Features.AutoReply
{
    public class AutoReplyDto
    {
        public int Id { get; set; }

        /// <summary>Null = the default row used for every client without its own settings.</summary>
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }

        public bool IsEnabled { get; set; } = true;
        public string? InstantMessage { get; set; }
        public string StartTime { get; set; } = "00:00";
        public string EndTime { get; set; } = "00:00";

        /// <summary>DayOfWeek numbers (0 = Sunday). Empty = every day.</summary>
        public List<int> ActiveDays { get; set; } = new();
        public string? ScheduledMessage { get; set; }

        public DateTime? DateUpdated { get; set; }
        public string? UpdatedByName { get; set; }

        /// <summary>Set by the API: whether the scheduled window is running right now.</summary>
        public bool IsWindowActiveNow { get; set; }
    }
}

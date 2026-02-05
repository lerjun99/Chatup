namespace ChatUp.Services.DTOs
{

    public class TicketDto
    {
        public int Id { get; set; }
        public string TicketNo { get; set; } = "";
        public DateTime DateReceived { get; set; }
        public string IssueTitle { get; set; } = "";
        public string Concern { get; set; } = "";
        public string RequestedByName { get; set; } = "";   // resolved user name
        public string ClientName { get; set; } = "";       
        public int ProjectId { get; set; } 
        public string ProjectName { get; set; } = "";
        public string SupportedByName { get; set; } = "";   // resolved support staff
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";

        // 🔹 SLA tracking
        public DateTime DueDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public bool IsBreached { get; set; }

        public bool IsArchived { get; set; } = false;
    }
}

namespace ChatUp.Services.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }   // readable name from navigation
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}

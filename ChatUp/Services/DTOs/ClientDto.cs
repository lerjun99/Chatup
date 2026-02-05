namespace ChatUp.Services.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public string Location { get; set; }
        public string PhotoUrl { get; set; }
        public string? EmailAddress { get; set; }
        public string? TelephoneNumber { get; set; }


    }
}

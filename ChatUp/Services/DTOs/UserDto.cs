namespace ChatUp.Services.DTOs
{
    class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string AvatarUrl { get; set; } = "images/default.png";
    }
}

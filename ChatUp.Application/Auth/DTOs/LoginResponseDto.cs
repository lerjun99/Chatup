namespace ChatUp.Application.Auth.DTOs;

public class LoginResponseDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public int UserType { get; set; }
    public int ClientId { get; set; }
    public string? Token { get; set; }
    public string? AvatarUrl { get; set; }
}
namespace ChatUp.Shared.Auth;

public class AuthResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class LoginResponseDto
{
    public int Id { get; set; } 
    public string Username { get; set; } = string.Empty;
    public int UserType { get; set; }
    public string Token { get; set; } = string.Empty;
}
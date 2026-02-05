namespace ChatUp.Application.Auth.DTOs;


public class AuthResponseDto<T>
{
    public BaseResult? BaseResult { get; set; }
    public T? Data { get; set; }
}
public class BaseResult
{
    public string? Code { get; set; }
    public string? Status { get; set; }
    public string? MsgCode { get; set; }
    public string? Msg { get; set; }
    public string? Ref { get; set; }
    public bool firstLogin { get; set; }
}
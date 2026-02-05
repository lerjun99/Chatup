using ChatUp.Domain.Entities;

namespace ChatUp.Application.Auth.DTOs;

public class ChatMessageDto
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Text { get; set; } = string.Empty;
}
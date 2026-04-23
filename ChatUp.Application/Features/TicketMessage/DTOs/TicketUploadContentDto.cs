namespace ChatUp.Application.Features.TicketMessage.DTOs;

public sealed class TicketUploadContentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Base64Content { get; set; } = string.Empty;
    public string? ThumbnailBase64 { get; set; }
}

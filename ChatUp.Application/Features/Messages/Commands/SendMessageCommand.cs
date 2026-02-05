using ChatUp.Application.Auth.DTOs;
using ChatUp.Application.Features.Messages.DTOs;
using MediatR;

namespace ChatUp.Application.Features.Messages.Commands
{
    //public record SendMessageCommand(ChatMessageDto MessageDto);
    public class SendMessageCommand : IRequest<int>
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
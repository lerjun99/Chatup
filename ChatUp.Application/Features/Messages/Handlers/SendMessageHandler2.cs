using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Features.Messages.Commands;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;

namespace ChatUp.Application.Features.Messages.Handlers
{
    public class SendMessageHandler
    {
        private readonly IMessageRepository _repository;

        public SendMessageHandler(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<ChatMessage> Handle(SendMessageCommand command)
        {
            var message = new ChatMessage
            {
                SenderId = command.MessageDto.SenderId,
                ReceiverId = command.MessageDto.ReceiverId,
                Text = command.MessageDto.Text,
                Timestamp = DateTime.UtcNow
            };

            return await _repository.AddMessageAsync(message);
        }
    }
}

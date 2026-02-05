using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Auth.DTOs;
using ChatUp.Application.Features.Messages.Commands;
using ChatUp.Application.Features.Messages.DTOs;
using ChatUp.Application.Features.Messages.Handlers;
using ChatUp.Application.Features.Messages.Queries;
using ChatUp.Domain.Entities;
using ChatUp.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{

    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class MessagingController : ControllerBase
    {
        private readonly GetConversationHandler _conversationHandler;
        private readonly SendMessageCommandHandler _sendMessageHandler;
        private readonly NotificationService _notificationService;
        private readonly IMediator _mediator;

        public MessagingController(
            GetConversationHandler conversationHandler,
            SendMessageCommandHandler sendMessageHandler,
            NotificationService notificationService,
            IMediator mediator)
        {
            _conversationHandler = conversationHandler;
            _sendMessageHandler = sendMessageHandler;
            _notificationService = notificationService;
            _mediator = mediator;
        }

        [HttpGet("conversation/{user1Id}/{user2Id}")]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetConversation(int user1Id, int user2Id, int skip, int take)
        {
            var messages = await _conversationHandler.Handle(new GetConversationQuery( user1Id,  user2Id,  skip ,  take ));
            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (dto == null)
                return BadRequest("Message cannot be null");

            var command = new SendMessageCommand
            {
                ConversationId = dto.ConversationId,
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverId,
                Text = dto.Text
            };

            var messageId = await _mediator.Send(command);

            return Ok(new { MessageId = messageId });
        }
        [HttpPost]
        public async Task<IActionResult> CloseConversation([FromBody] CloseConversationDto dto)
        {
            var command = new CloseConversationCommand
            {
                ConversationId = dto.ConversationId,
                RequestedBy = dto.RequestedBy
            };

            await _mediator.Send(command);
            return Ok(new { Success = true, Message = "Conversation closed" });
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateConversation([FromBody] ChatConversationDto dto)
        {
            if (dto == null || dto.UserIds.Count == 0)
                return BadRequest("Conversation must have a name and at least one participant.");

            var command = new CreateChatCommand
            {
                Name = dto.Name,
                IsGroup = dto.IsGroup,
                UserIds = dto.UserIds
            };

            var conversationId = await _mediator.Send(command);
            return Ok(new { ConversationId = conversationId });
        }

    }
}

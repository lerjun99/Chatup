using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using ChatUp.Application.Tickets.Commands.DeleteTicket;
using ChatUp.Application.Tickets.Commands.UpdateTicket;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class TicketMessageController : ControllerBase
    {
        private readonly IMediator _mediator;
        public TicketMessageController(IMediator mediator) => _mediator = mediator;

        [HttpGet("GetTickets")]
        public async Task<ActionResult<TicketPagedResponse>> GetTickets(
    [FromQuery] int userId,
    [FromQuery] string? search = null,
    [FromQuery] TicketStatus? status = null,
    [FromQuery] TicketPriority? priority = null,
    [FromQuery] bool includeMessages = false,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
        {
            var query = new GetTicketsQueries(userId, search, status, priority, includeMessages, page, pageSize);
            var ticketPagedResponse = await _mediator.Send(query);

            if (ticketPagedResponse == null || ticketPagedResponse.Tickets.Count == 0)
                return NotFound("No tickets found for the given filters.");

            return Ok(ticketPagedResponse);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDto>> GetTicket(int id)
        {
            var ticket = await _mediator.Send(new GetTicketByIdQueries(id));
            if (ticket == null) return NotFound();
            return Ok(ticket);
        }

        [HttpPost]
        public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] CreateTicketRequest request)
        {
            var cmd = new Application.Features.TicketMessage.Commands.CreateTicketCommand(
                request.IssueTitle,
                request.ProjectId,
                request.ClientId,
                request.RequestedById,
                request.DueDate,
                request.InitialMessage
            );
            var created = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetTicket), new { id = created.Id }, created);
        }

        [HttpPost("{id:int}/messages")]
        public async Task<ActionResult<MessageDto>> AddMessage(int id, [FromBody] AddMessageRequest req)
        {
            // Use object initializer to match command
            var msg = await _mediator.Send(new AddTicketMessageCommand
            {
                TicketId = id,
                SenderId = req.UserId,
                IsUser = req.IsUser,
                Content = req.Content,
                Attachments = req.Attachments // optional
            });

            return Ok(msg);
        }
        [HttpGet("{ticketId:int}/conversation")]
        public async Task<ActionResult<TicketConversationDto>> GetConversation(
       int ticketId,
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            // Call your MediatR handler with pagination
            var query = new LoadTicketConversationQuery(ticketId, page, pageSize);
            var conversation = await _mediator.Send(query);

            // If no messages, return empty DTO instead of 404
            if (conversation == null)
            {
                conversation = new TicketConversationDto
                {
                    Messages = new List<MessageDto>(),
                    ActiveUsers = new List<ActiveUserDto>(),
                    TotalMessages = 0,
                    PageNumber = page,
                    PageSize = pageSize
                };
            }

            // Optional: check if fewer messages than pageSize → no more messages
            conversation.PageNumber = page;
            conversation.PageSize = pageSize;
            conversation.TotalMessages = conversation.TotalMessages; // already set by handler

            return Ok(conversation);
        }
        [HttpPut("update-title")]
        public async Task<IActionResult> UpdateTitle([FromBody] UpdateTicketTitleDto dto)
        {
            var result = await _mediator.Send(new UpdateTicketTitleCommand(dto.TicketId, dto.NewTitle));
            if (!result) return NotFound();
            return Ok();
        }
        [HttpPost("SubmitRating")]
        public async Task<IActionResult> SubmitRating([FromBody] SubmitTicketRatingCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result)
                return BadRequest("Failed to submit rating.");

            return Ok(new { Message = "Rating submitted successfully!" });
        }
        [HttpGet("GetTicketRating/{ticketId}")]
        public async Task<IActionResult> GetTicketRating(int ticketId)
        {
            var rating = await _mediator.Send(new GetTicketRatingQuery(ticketId));
            if (rating is null)
                return NotFound("Ticket not found or not rated yet.");

            return Ok(rating);
        }
        [HttpDelete("{uploadId}")]
        public async Task<IActionResult> DeleteUpload(int ticketId, int uploadId)
        {
            var result = await _mediator.Send(new DeleteTicketUploadCommand(uploadId));

            if (!result)
                return NotFound(new { message = "Upload not found" });

            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(int ticketId, [FromBody] TicketUploadDto model)
        {
            var cmd = new UploadTicketFileCommand(
             TicketId: model.TicketId,
             UploadedById: model.UploadedById,
             FileName: model.FileName,
             FileType: model.FileType,
             Base64Content: model.Base64Content,
             TicketMessageId: model.TicketMessageId // ✅ now works
         );

            var id = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetUploads), new { ticketId }, new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetUploads(int ticketId)
        {
            var items = await _mediator.Send(new GetTicketUploadsQuery(ticketId));
            return Ok(items);
        }
        public record CreateTicketRequest(string IssueTitle, int ProjectId, int? ClientId, int RequestedById, DateTime DueDate, string InitialMessage);


    }
}

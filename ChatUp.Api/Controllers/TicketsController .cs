using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Application.Features.Ticket.Queries;
using ChatUp.Application.Tickets.Commands.DeleteTicket;
using ChatUp.Application.Tickets.Commands.UpdateTicket;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IChatHubContext _chatHub;
        public TicketsController(IMediator mediator, IChatHubContext chatHub)
        {
            _mediator = mediator;
            _chatHub = chatHub;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketCommand cmd)
        {
            var result = await _mediator.Send(cmd);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },   // matches [HttpGet("{id}")]
                result
            );
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _mediator.Send(new GetTicketByIdQuery(id));
            if (ticket == null) return NotFound();
            return Ok(ticket);
        }

        [HttpGet]
        public async Task<IActionResult> Query(
              [FromQuery] int userId,
              [FromQuery] string? search,
              [FromQuery] int? statusFilter,
              [FromQuery] int? priorityFilter,
              [FromQuery] bool includeArchived = false,
              [FromQuery] int page = 1,
              [FromQuery] int pageSize = 10)
        {
            var query = new GetTicketsQuery(
                userId,
                search,
                (TicketStatus?)statusFilter,
                (TicketPriority?)priorityFilter,
                includeArchived,
                page,
                pageSize
            );

            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketCommand cmd)
        {
            if (id != cmd.Id) return BadRequest("Id mismatch");
            await _mediator.Send(cmd);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteTicketCommand(id));
            return NoContent();
        }
        [HttpGet("{ticketId:int}")]
        public async Task<IActionResult> GetHistory(int ticketId)
        {
            var result = await _mediator.Send(new GetTicketHistoryQuery(ticketId));
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateTicketStatusCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            if (!result) return NotFound();
            await _chatHub.NotifyTicketUpdated();
            return Ok(new { Message = "Status updated successfully." });
        }
        [HttpPost("Restore/{id}")]
        public async Task<IActionResult> RestoreTicket(int id)
        {
            var result = await _mediator.Send(new RestoreTicketCommand(id));
            if (!result)
                return NotFound(new { Message = "Ticket not found or not archived." });

            return Ok(new { Message = "Ticket restored successfully." });
        }
        [HttpPut]
        public async Task<IActionResult> UpdatePriority([FromBody] UpdateTicketPriorityCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { Message = "Ticket not found." });

            return Ok(new { Message = "Priority updated successfully." });
        }
        [HttpGet("ticket-summary")]
        [ProducesResponseType(typeof(TicketDashboardDto), 200)]
        public async Task<IActionResult> GetTicketSummary()
        {
            var result = await _mediator.Send(new GetTicketDashboardSummaryQuery());
            return Ok(result);
        }
        [HttpPut("UpdateIsCase/{ticketId}")]
        public async Task<IActionResult> UpdateIsCase(int ticketId, [FromQuery] int updatedBy)
        {
            var command = new UpdateTicketIsCaseCommand
            {
                TicketId = ticketId,
                UpdatedBy = updatedBy
            };

            var result = await _mediator.Send(command);

            if (!result) return NotFound();

            return Ok(new { Message = "Ticket updated to case successfully." });
        }
    }
}

using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Application.Features.Ticket.Queries;
using ChatUp.Application.Features.UserApplicant.Commands;
using ChatUp.Application.Features.UserApplicant.DTOs;
using ChatUp.Application.Features.UserApplicant.Queries;
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
    public class ApplicantController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IChatHubContext _chatHub;
        public ApplicantController(IMediator mediator, IChatHubContext chatHub)
        {
            _mediator = mediator;
            _chatHub = chatHub;
        }
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            await _mediator.Send(new RestoreApplicantCommand(id));
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateApplicantDto dto)
        {
            var result = await _mediator.Send(
                new CreateApplicantCommand { Dto = dto });

            if (!result.Success)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }
        [HttpGet]
        public async Task<ActionResult<List<ApplicantListDto>>> GetAll()
        {
            return Ok(await _mediator.Send(new GetApplicantsQuery()));
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<UpdateApplicantStatusResponseDto>> UpdateStatus(
       int id,
       [FromBody] UpdateApplicantStatusDto dto)
        {
            var result = await _mediator.Send(
                new UpdateApplicantStatusCommand(id, dto));

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteApplicantCommand(id));
            return NoContent();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateApplicantInfoDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid applicant data.");

            try
            {
                var command = new UpdateApplicantCommand(id, dto); // pass parameters here
                var result = await _mediator.Send(command);

                if (result)
                    return Ok(new { Message = "Applicant updated successfully" });

                return StatusCode(500, "Failed to update applicant");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var query = new GetApplicantByIdQuery(id);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var result = await _mediator.Send(new GetDashboardSummaryQuery
            {
                FromDate = from,
                ToDate = to
            });

            return Ok(result);
        }
    }
}

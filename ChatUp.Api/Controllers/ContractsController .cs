using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Features.Contracts.Commands;
using ChatUp.Application.Features.Contracts.Queries;
using DocumentFormat.OpenXml.Office.Word;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContractsController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpPost]
        public async Task<IActionResult> Create(CreateContractCommand command)
        {
            var res = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateContractCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID in route does not match body.");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteContractCommand { Id = id });
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contract = await _mediator.Send(new GetContractByIdQuery(id));
            if (contract == null) return NotFound();
            return Ok(contract);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var contracts = await _mediator.Send(new GetAllContractsQuery());
            return Ok(contracts);
        }

        [HttpPost("send-emails")]
        public async Task<IActionResult> SendScheduledEmails(SendScheduledEmailsCommand command)
        {
            await _mediator.Send(command);
            return Ok("Emails processed (check console for simulation).");
        }
    }
}

using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Auth.Commands.ChatUp.Application.Features.LoginHistory.Commands;
using ChatUp.Application.Auth.Queries;
using ChatUp.Application.Features.LoginHistory.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class LoginHistoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LoginHistoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login/{userId}")]
        public async Task<IActionResult> RecordLogin(int userId)
        {
            await _mediator.Send(new RecordLoginCommand(userId));
            return Ok();
        }

        [HttpPost("logout/{userId}")]
        public async Task<IActionResult> RecordLogout(int userId)
        {
            await _mediator.Send(new RecordLogoutCommand(userId));
            return Ok();
        }

        [HttpGet("last/{userId}")]
        public async Task<IActionResult> GetLastLogin(int userId)
        {
            var result = await _mediator.Send(new GetLastLoginQuery(userId));
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}

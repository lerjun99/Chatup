using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Features.User.Commands;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Application.Features.UserRegistration.Commands;
using ChatUp.Application.Features.UserRegistration.Handlers;
using ChatUp.Application.Features.UserRegistration.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetUserById(int id, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetSupportUserById(int id, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(new GetSupportUserByIdQuery(id), cancellationToken);

            if (user == null)
                return NotFound();

            return Ok(user);
        }
        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetUsers(CancellationToken cancellationToken)
        {
            var users = await _mediator.Send(new GetUsersQuery(), cancellationToken);
            return Ok(users);
        }
        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetUsersByClient(int clientId)
        {
            var users = await _mediator.Send(new GetUsersByClientQuery(clientId));
            return Ok(users);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
        Ok(await _mediator.Send(new GetUserClientAssignmentsByClientIdQuery()));
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var result = await _mediator.Send(new GetAllUserClientAssignmentsQuery());
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserClientAssignmentCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // 🔹 Return a proper 400 with message
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // 🔹 Fallback for any unexpected error
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateUserClientAssignmentCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) =>
            Ok(await _mediator.Send(new DeleteUserClientAssignmentCommand { Id = id }));
        [HttpPost("create")]
        public async Task<IActionResult> Register([FromBody] CreateUserCommand command)
           => Ok(await _mediator.Send(command));

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserCommand command)
            => Ok(await _mediator.Send(command));

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
            => Ok(await _mediator.Send(new DeleteUserCommand { Id = id }));
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
           => Ok(await _mediator.Send(command));
        [HttpGet]
        public async Task<ActionResult<List<UserDetailsDto>>> GetAllUsers(CancellationToken cancellationToken)
        {
            var users = await _mediator.Send(new GetUsersWithDetailsQuery(), cancellationToken);
            return Ok(users);
        }
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDetailsDto>> GetUser(int userId, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(new GetUserDetailsQuery(userId), cancellationToken);
            if (user == null) return NotFound();
            return Ok(user);
        }
        [HttpPost("Approve")]
        public async Task<IActionResult> ApproveUser([FromBody] ApproveUserCommand command, CancellationToken cancellationToken)
        {
            if (command == null || command.UserId <= 0)
                return BadRequest(new { message = "Invalid user ID." });

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }
        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetClientUsers(int clientId)
        {
            var users = await _mediator.Send(new GetClientUsersQuery(clientId));
            return Ok(users);
        }

    }
}

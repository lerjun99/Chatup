using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Features.Projects.Commands;
using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Application.Features.Projects.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProjectsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> Get(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProjectsQuery(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> Get(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }
        [HttpGet("{clientId}")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetByClient(int clientId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProjectsByClientQuery(clientId), cancellationToken);
            return Ok(result);
        }
        [HttpPost]
        public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
        {
            var created = await _mediator.Send(new CreateProjectCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
        {
            await _mediator.Send(new UpdateProjectCommand(id, dto), cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromBody] DeleteProjectDto dto, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteProjectCommand(id, dto), cancellationToken);
            return NoContent();
        }
        [HttpPost]
        public async Task<IActionResult> AssignUserToProject([FromBody] CreateUserProjectAssignmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var command = new CreateUserProjectAssignmentCommand(dto);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("Update")]
        public async Task<ActionResult<UserProjectAssignmentDto>> UpdateUserProjectAssignment([FromBody] UpdateUserProjectAssignmentCommand command)
        {
            if (command == null)
                return BadRequest("Invalid request");

            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
        [HttpDelete("Assignment/{id}")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var result = await _mediator.Send(new DeleteUserProjectAssignmentCommand(id));

            if (!result)
                return NotFound();

            return Ok(new { message = "Assignment deleted successfully." });
        }

    }
}

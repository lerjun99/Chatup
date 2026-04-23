using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Features.Dashboard.DTOs;
using ChatUp.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatUp.Api.Controllers
{
    //[Authorize("ApiKey")]
    [Route("[controller]/[action]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] int userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetDashboardQuery
            {
                UserId = userId,
                From = from,
                To = to,
                SlaAlertsPage = page,
                SlaAlertsPageSize = pageSize
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetActivity([FromQuery] int userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new ChatUp.Application.Features.Activity.Queries.GetActivityLogsQuery
            {
                UserId = userId,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            });

            return Ok(result);
        }

    }
}

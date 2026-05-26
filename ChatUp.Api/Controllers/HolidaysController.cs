using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Holiday.Commands;
using ChatUp.Application.Features.Holiday.Queries;
using ChatUp.Application.Features.User.Commands;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Application.Features.UserRegistration.Commands;
using ChatUp.Application.Features.UserRegistration.Handlers;
using ChatUp.Application.Features.UserRegistration.Queries;
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
    public class HolidaysController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHolidayRepository _repo;
        public HolidaysController(IMediator mediator, IHolidayRepository repo)
        {
            _repo = repo;
            _mediator = mediator;
        }

        // =========================
        // 📌 GET HOLIDAYS (DB + SLA READY)
        // =========================
        [HttpGet("{region}")]
        public async Task<IActionResult> Get(string region)
        {
            var result = await _mediator.Send(new GetHolidaysQuery(region));
            return Ok(result);
        }

        // =========================
        // 📌 SYNC HOLIDAYS (API → DB)
        // =========================
        [HttpPost("sync/{region}/{year}")]
        public async Task<IActionResult> Sync(string region, int year)
        {
            await _mediator.Send(new SyncHolidaysCommand(region, year));

            return Ok(new
            {
                message = $"Holidays synced successfully for {region} - {year}"
            });
        }

        // =========================
        // 📌 SLA CALENDAR SNAPSHOT (FOR FRONTEND / SIGNALR)
        // =========================
        [HttpGet("sla-calendar/{region}")]
        public async Task<IActionResult> GetSlaCalendar(string region)
        {
            var holidays = await _repo.GetByRegionAsync(region);

            var calendar = new
            {
                Region = region,
                WorkStart = new TimeSpan(9, 0, 0),
                WorkEnd = new TimeSpan(18, 0, 0),

                WorkingDays = new[]
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                },

                Holidays = holidays.Select(h => new
                {
                    h.Date,
                    h.Name
                })
            };

            return Ok(calendar);
        }

        // =========================
        // 📌 SLA CHECK (REAL-TIME CALCULATION)
        // =========================
        [HttpPost("sla/check")]
        public async Task<IActionResult> CheckSla([FromBody] SlaRequest request)
        {
            var holidays = await _repo.GetByRegionAsync(request.Region);

            var calendar = new BusinessCalendar
            {
                WorkStart = new TimeSpan(9, 0, 0),
                WorkEnd = new TimeSpan(18, 0, 0),

                WorkingDays = new List<DayOfWeek>
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                },

                Holidays = holidays.Select(h => h.Date).ToList()
            };

            var remaining = BusinessTimeHelper.GetBusinessTimeRemaining(
                request.StartDate,
                request.DueDate,
                calendar);

            return Ok(new
            {
                RemainingBusinessTime = remaining,
                IsBreached = remaining <= TimeSpan.Zero
            });
        }

        public class SlaRequest
        {
            public string Region { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
        }
    }
}

using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Holiday.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ChatUp.Application.Features.Holiday.Handlers
{
    public class SyncHolidaysCommandHandler
        : IRequestHandler<SyncHolidaysCommand, Unit>
    {
        private readonly IHolidayApiService _api;
        private readonly IHolidayRepository _repo;

        public SyncHolidaysCommandHandler(
            IHolidayApiService api,
            IHolidayRepository repo)
        {
            _api = api;
            _repo = repo;
        }

        public async Task<Unit> Handle(SyncHolidaysCommand request, CancellationToken ct)
        {
            if (request.Region != "PH")
                return Unit.Value;

            var apiHolidays = await _api.FetchPhilippineHolidays(request.Year);
            var existing = await _repo.GetByRegionAsync("PH");

            var newHolidays = apiHolidays
                .Where(h => !existing.Any(e => e.Date == h.Date))
                .ToList();

            if (newHolidays.Any())
            {
                await _repo.AddRangeAsync(newHolidays);
                await _repo.SaveChangesAsync();
            }

            return Unit.Value;
        }
    }
}

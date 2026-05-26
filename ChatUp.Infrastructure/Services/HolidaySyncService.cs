using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class HolidaySyncService
    {
        private readonly IHolidayRepository _repo;
        private readonly HolidayApiService _api;

        public HolidaySyncService(IHolidayRepository repo, HolidayApiService api)
        {
            _repo = repo;
            _api = api;
        }

        public async Task SyncPhilippineHolidays(int year)
        {
            var apiHolidays = await _api.FetchPhilippineHolidays(year);

            var newHolidays = new List<Holiday>();

            foreach (var holiday in apiHolidays)
            {
                if (!await _repo.ExistsAsync(holiday.Date, "PH"))
                {
                    newHolidays.Add(holiday);
                }
            }

            if (newHolidays.Any())
            {
                await _repo.AddRangeAsync(newHolidays);
                await _repo.SaveChangesAsync();
            }
        }
    }
}

using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class HolidayApiService : IHolidayApiService
    {
        private readonly HttpClient _http;

        public HolidayApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Holiday>> FetchPhilippineHolidays(int year)
        {
            var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/PH";

            var response = await _http.GetFromJsonAsync<List<NagerHolidayDto>>(url);

            return response.Select(x => new Holiday
            {
                Date = x.Date,
                Name = x.LocalName,
                Region = "PH"
            }).ToList();
        }

        private class NagerHolidayDto
        {
            public DateTime Date { get; set; }
            public string LocalName { get; set; }
        }
    }
}

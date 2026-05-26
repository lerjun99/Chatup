using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;

namespace ChatUp.Infrastructure.Services
{
    public class BusinessCalendarService : IBusinessCalendarService
    {
        private readonly IBusinessCalendarRepository _repo;

        public BusinessCalendarService(IBusinessCalendarRepository repo)
        {
            _repo = repo;
        }

        public async Task<BusinessCalendar> GetCalendarAsync(string countryCode)
        {
            var calendar = await _repo.GetByCountryAsync(countryCode);

            if (calendar == null)
                throw new Exception($"Business calendar not found for {countryCode}");

            return calendar;
        }
    }
}
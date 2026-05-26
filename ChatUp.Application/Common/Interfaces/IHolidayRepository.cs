using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IHolidayRepository
    {
        Task<List<Holiday>> GetByRegionAsync(string region);
        Task AddRangeAsync(List<Holiday> holidays);
        Task<bool> ExistsAsync(DateTime date, string region);
        Task SaveChangesAsync();
    }
}

using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence.Repositories
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly ChatDBContext _context;

        public HolidayRepository(ChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<Holiday>> GetByRegionAsync(string region)
        {
            return await _context.Holidays
                .Where(x => x.Region == region)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(DateTime date, string region)
        {
            return await _context.Holidays
                .AnyAsync(x => x.Date == date.Date && x.Region == region);
        }

        public async Task AddRangeAsync(List<Holiday> holidays)
        {
            await _context.Holidays.AddRangeAsync(holidays);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

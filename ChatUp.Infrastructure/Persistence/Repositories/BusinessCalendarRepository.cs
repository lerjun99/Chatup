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
    public class BusinessCalendarRepository : IBusinessCalendarRepository
    {
        private readonly ChatDBContext _context;

        public BusinessCalendarRepository(ChatDBContext context)
        {
            _context = context;
        }

        public async Task<BusinessCalendar?> GetByCountryAsync(string countryCode)
        {
            return await _context.BusinessCalendar
       .FirstOrDefaultAsync(x => x.Region.ToLower() == countryCode.ToLower());
        }
    }
}

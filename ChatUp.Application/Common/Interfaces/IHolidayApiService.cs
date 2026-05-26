using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IHolidayApiService
    {
        Task<List<Holiday>> FetchPhilippineHolidays(int year);
    }
}

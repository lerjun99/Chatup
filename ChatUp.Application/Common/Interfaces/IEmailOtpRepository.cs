using ChatUp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IEmailOtpRepository
    {
        Task<EmailOtp?> GetByEmailAsync(string email);
        Task AddAsync(EmailOtp otp);
        Task UpdateAsync(EmailOtp otp);
    }

}

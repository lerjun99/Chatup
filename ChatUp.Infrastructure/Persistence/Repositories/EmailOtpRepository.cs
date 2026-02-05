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
    public class EmailOtpRepository : IEmailOtpRepository
    {
        private readonly ChatDBContext _context;

        public EmailOtpRepository(ChatDBContext context)
        {
            _context = context;
        }

        public async Task<EmailOtp?> GetByEmailAsync(string email)
        {
            return await _context.EmailOtp
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task AddAsync(EmailOtp otp)
        {
            await _context.EmailOtp.AddAsync(otp);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmailOtp otp)
        {
            _context.EmailOtp.Update(otp);
            await _context.SaveChangesAsync();
        }
    }
}

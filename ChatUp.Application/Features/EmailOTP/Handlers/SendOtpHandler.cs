using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.EmailOTP.Commands;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.EmailOTP.Handlers
{
    public class SendOtpHandler : IRequestHandler<SendOtpCommand, bool>
    {
        private readonly IUserRepository _userRepo;
        private readonly IEmailOtpRepository _otpRepo;
        private readonly IEmailService _email;

        public SendOtpHandler(
            IUserRepository userRepo,
            IEmailOtpRepository otpRepo,
            IEmailService email)
        {
            _userRepo = userRepo;
            _otpRepo = otpRepo;
            _email = email;
        }

        public async Task<bool> Handle(SendOtpCommand request, CancellationToken ct)
        {
            if (!await _userRepo.EmailExistsAsync(request.Email))
                return false;

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var hash = BCrypt.Net.BCrypt.HashPassword(otp);

            var record = await _otpRepo.GetByEmailAsync(request.Email);

            if (record == null)
            {
                record = new EmailOtp
                {
                    Email = request.Email,
                    OtpHash = hash,
                    Expiry = DateTime.UtcNow.AddMinutes(5),
                    CreatedAt = DateTime.UtcNow
                };
                await _otpRepo.AddAsync(record);
            }
            else
            {
                record.OtpHash = hash;
                record.Expiry = DateTime.UtcNow.AddMinutes(5);
                record.IsVerified = false;
                record.FailedAttempts = 0;
                await _otpRepo.UpdateAsync(record);
            }

            await _email.SendEmailAsync(
                request.Email,
                "Password Reset Verification Code",
                EmailTemplates.OtpEmail(request.Email, otp)
            );

            return true;
        }
    }
}

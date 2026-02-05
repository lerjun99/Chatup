using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.EmailOTP.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.EmailOTP.Handlers
{
    public class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, bool>
    {
        private readonly IEmailOtpRepository _otpRepo;

        public VerifyOtpHandler(IEmailOtpRepository otpRepo)
        {
            _otpRepo = otpRepo;
        }

        public async Task<bool> Handle(VerifyOtpCommand request, CancellationToken ct)
        {
            var otp = await _otpRepo.GetByEmailAsync(request.Email);

            if (otp == null || otp.Expiry < DateTime.UtcNow)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(request.Code, otp.OtpHash))
            {
                otp.FailedAttempts++;
                await _otpRepo.UpdateAsync(otp);
                return false;
            }

            otp.IsVerified = true;
            await _otpRepo.UpdateAsync(otp);

            return true;
        }
    }

}

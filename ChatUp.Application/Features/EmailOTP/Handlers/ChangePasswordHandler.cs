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
    public class ChangePasswordHandler : IRequestHandler<ChangeLogPasswordCommand, bool>
    {
        private readonly IUserRepository _userRepo;
        private readonly IEmailOtpRepository _otpRepo;

        public ChangePasswordHandler(
            IUserRepository userRepo,
            IEmailOtpRepository otpRepo)
        {
            _userRepo = userRepo;
            _otpRepo = otpRepo;
        }

        public async Task<bool> Handle(ChangeLogPasswordCommand request, CancellationToken ct)
        {
            var otp = await _otpRepo.GetByEmailAsync(request.Email);
            if (otp == null || !otp.IsVerified)
                return false;

            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null) return false;

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepo.UpdateAsync(user);

            return true;
        }
    }

}

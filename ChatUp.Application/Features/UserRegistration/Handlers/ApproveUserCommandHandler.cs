using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserRegistration.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Handlers
{
    public class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, ChangePasswordResponseDto>
    {
        private readonly IChatDBContext _context;
        private readonly IEmailService _emailService;
        private readonly ICryptography _crypto;
        public ApproveUserCommandHandler(IChatDBContext context, IEmailService emailService, ICryptography crypto)
        {
            _context = context;
            _emailService = emailService;
            _crypto = crypto;
        }

        public async Task<ChangePasswordResponseDto> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return new ChangePasswordResponseDto
                {
                    IsSuccess = false,
                    Message = "User not found."
                };
            }

            // Generate temporary password
            string tempPassword = GenerateRandomPassword(10);
            var encryptedPassword = _crypto.Encrypt(tempPassword);
            user.Password = encryptedPassword;
            user.DateUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Send approval email
            string subject = "Your Account Has Been Approved";
            string body = $@"
                <p>Dear {user.FullName},</p>
                <p>Your account has been approved! You can now log in to the portal using the following temporary password:</p>
                <p><b>Username:</b> {user.Username}</p>
                <p><b>Temporary Password:</b> {tempPassword}</p>
                <p>Please log in at <a href='https://portal.odeccisolutions.com'>portal.odeccisolutions.com</a> 
                and change your password immediately after signing in.</p>
                <p>Thank you,<br/>ODECCI Solutions Team</p>";

            await _emailService.SendEmailAsync(user.EmailAddress, subject, body);

            return new ChangePasswordResponseDto
            {
                IsSuccess = true,
                TemporaryPassword = tempPassword,
                Message = "User approved and temporary password sent successfully."
            };
        }

        private static string GenerateRandomPassword(int length)
        {
            const string valid = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789@#$";
            StringBuilder res = new StringBuilder();
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];
                while (res.Length < length)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }
            return res.ToString();
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}

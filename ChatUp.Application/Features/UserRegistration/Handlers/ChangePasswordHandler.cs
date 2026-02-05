using BCrypt.Net;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserRegistration.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Handlers
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
    {
        private readonly IChatDBContext _context;
        private readonly ICryptography _crypto;
        public ChangePasswordHandler(IChatDBContext context, ICryptography crypto)
        {
            _context = context;
            _crypto = crypto;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.UserAccounts.FindAsync(new object[] { request.UserId }, cancellationToken);

            if (user == null)
                throw new InvalidOperationException("{\"message\":\"User not found.\"}");

            bool isPasswordValid;

            // ✅ Check if password is stored as BCrypt hash
            if (user.Password.StartsWith("$2"))
            {
                // Validate using BCrypt
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password);
            }
            else
            {
                // Validate using custom encryption (legacy support)
                var decryptedStoredPassword = _crypto.Decrypt(user.Password);
                isPasswordValid = decryptedStoredPassword == request.CurrentPassword;
            }

            if (!isPasswordValid)
                throw new InvalidOperationException("{\"message\":\"Current password is incorrect.\"}");

            // ✅ Hash the new password with BCrypt
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.DateUpdated = DateTime.Now;
            user.IsFirstLogIn = false;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

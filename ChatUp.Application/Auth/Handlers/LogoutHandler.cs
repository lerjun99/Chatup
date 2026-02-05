using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.Handlers
{
    public class LogoutHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly IChatDBContext _context;
        private readonly IUserStatusNotifier _statusNotifier;
        public LogoutHandler(IChatDBContext context, IUserStatusNotifier statusNotifier)
        {
            _context = context;
            _statusNotifier = statusNotifier;
        }

        public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null) return false;

            // mark user as logged out
            user.isLoggedIn = 0;
            user.JWToken = null;
            user.DateUpdated = DateTime.UtcNow;
            _context.UserAccounts.Update(user);

            // update the last login history record
            var history = await _context.LoginHistories
                .Where(h => h.UserId == request.UserId && h.LogoutTime == null)
                .OrderByDescending(h => h.LoginTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (history != null)
            {
                history.LogoutTime = DateTime.UtcNow;
                _context.LoginHistories.Update(history);
            }
            var phZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phZone);
            var loginHistory = new LoginHistory
            {
                UserId = user.Id ?? 0,
                LogoutTime = localNow
            };
            _context.LoginHistories.Add(loginHistory);
            await _context.SaveChangesAsync(cancellationToken);
            await _statusNotifier.NotifyStatusChanged(user.Id ?? 0, false);
            return true;
        
    }
    }
}

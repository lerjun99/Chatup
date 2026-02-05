using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.LoginHistory.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.Handlers
{
    public class RecordLogoutCommandHandler : IRequestHandler<RecordLogoutCommand, Unit>
    {
        private readonly ILoginHistoryRepository _repo;

        public RecordLogoutCommandHandler(ILoginHistoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(RecordLogoutCommand request, CancellationToken cancellationToken)
        {
            var lastLogin = await _repo.GetLastLoginAsync(request.UserId, cancellationToken);
            var phZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phZone);
            if (lastLogin != null && lastLogin.LogoutTime == null)
            {
                lastLogin.LogoutTime = localNow;
                await _repo.UpdateAsync(lastLogin, cancellationToken);
            }
            return Unit.Value;
        }
    }
}

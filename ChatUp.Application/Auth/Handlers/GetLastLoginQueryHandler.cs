using ChatUp.Application.Auth.DTOs;
using ChatUp.Application.Auth.Queries;
using ChatUp.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.Handlers
{
    public class GetLastLoginQueryHandler : IRequestHandler<GetLastLoginQuery, LoginInfoDto?>
    {
        private readonly ILoginHistoryRepository _repo;

        public GetLastLoginQueryHandler(ILoginHistoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<LoginInfoDto?> Handle(GetLastLoginQuery request, CancellationToken cancellationToken)
        {
            var lastLogin = await _repo.GetLastLoginAsync(request.UserId, cancellationToken);
            if (lastLogin == null) return null;

            return new LoginInfoDto
            {
                LoginTime = lastLogin.LoginTime,
                LogoutTime = lastLogin.LogoutTime,
                DurationMinutes = lastLogin.Duration?.TotalMinutes
            };
        }
    }
}

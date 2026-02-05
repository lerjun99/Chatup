using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.User.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Handlers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginHistoryRepository _loginHistoryRepository;

        public GetUsersQueryHandler(
            IUserRepository userRepository,
            ILoginHistoryRepository loginHistoryRepository)
        {
            _userRepository = userRepository;
            _loginHistoryRepository = loginHistoryRepository;
        }

        public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetUsersAsync(cancellationToken);
            var userDtos = new List<UserDto>();

            foreach (var u in users)
            {
                // get last login history for this user
                var lastLogin = await _loginHistoryRepository.GetLastLoginAsync(u.Id ?? 0, cancellationToken);

                userDtos.Add(new UserDto
                {
                    Id = u.Id ?? 0,
                    Name = u.FullName,
                    EmailAddress = u.EmailAddress ?? "",
                    AvatarUrl = u.Uploads != null && u.Uploads.Any()
                        ? u.Uploads.First().Base64Content
                        : "images/default.png",
                    IsOnline = (u.isLoggedIn ?? 0) == 1 && (lastLogin != null && !lastLogin.LogoutTime.HasValue),
                    // new fields
                    LogoutTime = lastLogin?.LogoutTime,
                    LoginTime = lastLogin?.LoginTime,
                    DurationMinutes = lastLogin?.Duration?.TotalMinutes,
                    UserType= u.UserType
                });
            }

            return userDtos;
        }
    }
}

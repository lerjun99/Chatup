using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Domain.Entities;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Handlers
{
    public class GetSupportUserByIdQueryHandler : IRequestHandler<GetSupportUserByIdQuery, List<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginHistoryRepository _loginHistoryRepository;
        private readonly IUserClientRepository _userClientRepository;
        public GetSupportUserByIdQueryHandler(
            IUserRepository userRepository,
            ILoginHistoryRepository loginHistoryRepository, IUserClientRepository userClientRepository)
        {
            _userRepository = userRepository;
            _loginHistoryRepository = loginHistoryRepository;
            _userClientRepository = userClientRepository;
        }


        public async Task<List<UserDto>> Handle(GetSupportUserByIdQuery request, CancellationToken cancellationToken)
        {
           
            var currentUser = await _userRepository.GetUserByIdAsync(request.Id, cancellationToken);
            if (currentUser == null)
                return new List<UserDto>();

            var allUsers = await _userRepository.GetUsersAsync(cancellationToken);
            var allAssignments = await _userClientRepository.GetAllAssignmentsAsync(cancellationToken);

            var userDtos = new List<UserDto>();
            IEnumerable<UserAccount> filteredUsers;

            switch (currentUser.UserType)
            {
                case 1:
                    // 🧭 Admin – can see all users except self
                    filteredUsers = allUsers.Where(u => u.Id != currentUser.Id && u.UserType == 2);
                    break;

                case 2:
                    var myClientIds = allAssignments
                       .Where(a => a.UserId == currentUser.Id)
                       .Select(a => a.ClientId)
                       .Distinct()
                       .ToList();

                    filteredUsers = allUsers
                        .Where(u => u.Id != currentUser.Id && u.UserType == 3 &&
                                    allAssignments.Any(a =>
                                        a.ClientId == u.Id &&
                                        myClientIds.Contains(a.ClientId)))
                        .ToList();
                    break;

                case 3:
                    var mySupports = allAssignments
                              .Where(a => a.ClientId == currentUser.Id)
                              .Select(a => a.UserId)
                              .Distinct()
                              .ToList();

                    filteredUsers = allUsers
                        .Where(u => u.Id != currentUser.Id && mySupports.Contains(u.Id ?? 0))
                        .ToList();
                    break;

                default:
                    filteredUsers = Enumerable.Empty<UserAccount>();
                    break;
            }

            // 🧭 Enrich users with login status & activity
            foreach (var u in filteredUsers)
            {
                var lastLogin = await _loginHistoryRepository.GetLastLoginAsync(u.Id ?? 0, cancellationToken);
                bool isOnline = (u.isLoggedIn ?? 0) == 1 && (lastLogin != null && !lastLogin.LogoutTime.HasValue);

                string lastActiveStatus = "Offline";
                if (isOnline)
                {
                    lastActiveStatus = "Active now";
                }
                else if (lastLogin?.LoginTime != null)
                {
                    TimeSpan diff = DateTime.UtcNow - lastLogin.LoginTime.ToUniversalTime();

                    if (diff.TotalSeconds < 60)
                        lastActiveStatus = $"Active {Math.Floor(diff.TotalSeconds)}s ago";
                    else if (diff.TotalMinutes < 60)
                        lastActiveStatus = $"Active {Math.Floor(diff.TotalMinutes)}m ago";
                    else if (diff.TotalHours < 24)
                        lastActiveStatus = $"Active {Math.Floor(diff.TotalHours)}h ago";
                    else if (diff.TotalDays < 30)
                        lastActiveStatus = $"Active {Math.Floor(diff.TotalDays)}d ago";
                    else
                        lastActiveStatus = "Inactive for a while";
                }

                userDtos.Add(new UserDto
                {
                    Id = u.Id ?? 0,
                    Name = u.FullName,
                    EmailAddress = u.EmailAddress ?? "",
                    AvatarUrl = u.Uploads != null && u.Uploads.Any()
                        ? u.Uploads.First().Base64Content
                        : "images/default.png",
                    IsOnline = isOnline,
                    LoginTime = lastLogin?.LoginTime,
                    LogoutTime = lastLogin?.LogoutTime,
                    DurationMinutes = lastLogin?.Duration?.TotalMinutes,
                    LastActiveStatus = lastActiveStatus,
                    UserType=u.UserType
                });
            }

            return userDtos;
        }
    }
    }


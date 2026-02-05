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
    public class GetUsersByClientQueryHandler : IRequestHandler<GetUsersByClientQuery, List<UserDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetUsersByClientQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserDto>> Handle(GetUsersByClientQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetUsersByClientAsync(request.ClientId, cancellationToken);

            // Filter out admin users
            var filteredUsers = users
                .Where(u => u.UserType != 1) // exclude admin
                .ToList();

            return filteredUsers
                .Select(u => new UserDto
                {
                    Id = u.Id ?? 0,
                    EmailAddress = u.EmailAddress ?? "",
                    Name = u.FullName,
                    AvatarUrl = u.Uploads != null && u.Uploads.Any() ? u.Uploads.First().Base64Content : "images/default.png",
                    UnreadCount = 0
                })
                .ToList();
        }
    }
}

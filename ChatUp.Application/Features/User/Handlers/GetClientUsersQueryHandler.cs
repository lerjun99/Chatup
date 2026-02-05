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
    public class GetClientUsersQueryHandler : IRequestHandler<GetClientUsersQuery, List<UserClientDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetClientUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserClientDto>> Handle(GetClientUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetClientUsersAsync(request.ClientId);
            return users.Select(u => new UserClientDto
            {
                Id = u.Id!.Value,
                Username = u.Username,
                FullName = u.FullName,
                EmailAddress = u.EmailAddress,
                ClientId = u.ClientId,
                AvatarUrls = u.Uploads?.Select(x => x.Base64Content).ToList() ?? new List<string>()
            }).ToList();
        }
    }
}

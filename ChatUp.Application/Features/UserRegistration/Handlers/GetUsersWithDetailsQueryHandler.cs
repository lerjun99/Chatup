using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.UserRegistration.DTOs;
using ChatUp.Application.Features.UserRegistration.Queries;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Handlers
{
    public class GetUsersWithDetailsQueryHandler : IRequestHandler<GetUsersWithDetailsQuery, List<UserDetailsDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginHistoryRepository _loginHistoryRepository;

        public GetUsersWithDetailsQueryHandler(
            IUserRepository userRepository,
            ILoginHistoryRepository loginHistoryRepository)
        {
            _userRepository = userRepository;
            _loginHistoryRepository = loginHistoryRepository;
        }

        public async Task<List<UserDetailsDto>> Handle(GetUsersWithDetailsQuery request, CancellationToken cancellationToken)
        {
            var users = (await _userRepository.GetAllUsersWithDetailsAsync(cancellationToken))
         .OrderByDescending(u => u.Id)
         .ToList();
            var userDtos = new List<UserDetailsDto>();

            foreach (var user in users)
            {
                var userId = user.Id ?? 0;
                var lastLogin = await _loginHistoryRepository.GetLastLoginAsync(userId, cancellationToken);

                var dto = new UserDetailsDto
                {
                    Id = userId,
                    Username = user.Username,
                    FullName = user.FullName,
                    EmailAddress = user.EmailAddress,
                    IsActive = (user.IsActive ?? 0) == 1,
                    IsOnline = (user.isLoggedIn ?? 0) == 1 && (lastLogin != null && !lastLogin.LogoutTime.HasValue),
                    LastLoginTime = lastLogin?.LoginTime,
                    CreatedDate = user.DateCreated,
                    UserType = user.UserType,
                    Status = user.Status,

                    UploadedFiles = user.Uploads?.Select(u => new UploadedFileDto
                    {
                        Name = u.Name,
                        Base64Content = u.Base64Content,  // or Base64 if stored as such
                        FileType = u.FileType,
                        ContentType = u.ContentType,
                        Size = u.Size
                    }).ToList() ?? new List<UploadedFileDto>(),

                    ClientId = user.ClientId,
                    ClientName = user.Client?.ClientName ?? string.Empty,
                    IsClient = user.IsClient,

                    Contracts = user.UserContracts?.Select(c => new UserContractDto
                    {
                        Id = c.Contract?.Id ?? 0,
                        ContractName = c.Contract?.Title ?? string.Empty,
                        StartDate = c.Contract?.StartDate,
                        EndDate = c.Contract?.ExpirationDate
                    }).ToList() ?? new List<UserContractDto>(),

                    Projects = user.UserProjects?.Select(p => new UserProjectDto
                    {
                        Id = p.Project?.Id ?? 0,
                        ProjectName = p.Project?.Title ?? string.Empty,
                        Description = p.Project?.Description ?? string.Empty,
                        StartDate = p.Project?.Contract?.StartDate,
                        EndDate = p.Project?.Contract?.ExpirationDate
                    }).ToList() ?? new List<UserProjectDto>()
                };

                userDtos.Add(dto);
            }

            return userDtos;
        }
    }
}

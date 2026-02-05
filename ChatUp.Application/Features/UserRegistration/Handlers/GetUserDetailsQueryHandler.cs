using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.UserRegistration.DTOs;
using ChatUp.Application.Features.UserRegistration.Queries;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Handlers
{
    public class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, UserDetailsDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginHistoryRepository _loginHistoryRepository;
        private readonly IChatDBContext _context;
        public GetUserDetailsQueryHandler(
            IUserRepository userRepository,
            ILoginHistoryRepository loginHistoryRepository,IChatDBContext context)
        {
            _userRepository = userRepository;
            _loginHistoryRepository = loginHistoryRepository;
            _context = context;
        }

        public async Task<UserDetailsDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserWithDetailsAsync(request.UserId, cancellationToken);

            if (user == null)
                return null!; // or throw new NotFoundException("User not found.");

            var userId = user.Id ?? 0;
            var lastLogin = await _loginHistoryRepository.GetLastLoginAsync(userId, cancellationToken);

            // ✅ Fetch client assignments for this user
            var clientAssignments = await _context.UserClientAssignments
                .Where(x => x.UserId == userId)
                .Select(x => new UserClientAssignmentDto
                {
                    Id = x.Id,
                    ClientId = x.ClientId ?? 0,
                    ClientName = x.Client != null ? x.Client.ClientName : string.Empty
                })
                .ToListAsync(cancellationToken);

            // ✅ Map to DTO
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
                    Base64Content = u.Base64Content,
                    FileType = u.FileType,
                    ContentType = u.ContentType,
                    Size = u.Size
                }).ToList() ?? new List<UploadedFileDto>(),

                ClientId = user.ClientId,
                ClientName = user.Client?.ClientName ?? string.Empty,
                IsClient = user.IsClient,

                // ✅ Added section
                ClientAssignments = clientAssignments,

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

            return dto;
        }
    }
}

using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Contracts.DTOs;
using ChatUp.Application.Features.Contracts.Queries;
using ChatUp.Application.Features.User.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Handlers
{
    public class GetAllContractsHandler : IRequestHandler<GetAllContractsQuery, List<ContractDto>>
    {
        private readonly IChatDBContext _context;

        public GetAllContractsHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<ContractDto>> Handle(GetAllContractsQuery request, CancellationToken cancellationToken)
        {
            var contracts = await _context.Contracts
       .Include(c => c.Projects)
       .Include(c => c.UserContracts)
           .ThenInclude(uc => uc.UserAccount)
       .Include(c => c.Client) // ✅ ensures Client data (including Email) is loaded
       .AsNoTracking()
       .ToListAsync(cancellationToken);

            var result = new List<ContractDto>();

            foreach (var contract in contracts)
            {
                // ✅ Fetch all users related to the same client (if client exists)
                var userClients = new List<UserDto>();

                if (contract.ClientId != null)
                {
                    userClients = await _context.UserClients
                        .Include(uc => uc.User)
                            .ThenInclude(u => u.Uploads)
                        .Where(uc => uc.ClientId == contract.ClientId)
                        .Select(uc => new UserDto
                        {
                            Id = uc.User.Id ?? 0,
                            Name = uc.User.Username,
                            AvatarUrl = uc.User.Uploads
                                .OrderByDescending(u => u.Id)
                                .Select(u => u.Base64Content)
                                .FirstOrDefault()
                        })
                        .ToListAsync(cancellationToken);
                }

                // ✅ Safely map to DTO with Client.Email
                var dto = new ContractDto
                {
                    Id = contract.Id,
                    Title = contract.Title,
                    Description = contract.Description,
                    StartDate = contract.StartDate,
                    ExpirationDate = contract.ExpirationDate,
                    IsTerminated = contract.IsTerminated,
                    UserType = contract.UserContracts?.FirstOrDefault()?.UserType ?? 0,

                    ClientId = contract.ClientId ?? 0,
                    ClientName = contract.Client?.ClientName ?? string.Empty,
                    EmailAddress = contract.Client?.EmailAddress ?? string.Empty,  // ✅ Get email from Clients table

                    ProjectTitles = contract.Projects?.Select(p => p.Title).ToList() ?? new List<string>(),
                    ProjectIds = contract.Projects?.Select(p => p.Id).ToList() ?? new List<int>(),
                    UserIds = contract.UserContracts?.Select(uc => uc.UserAccountId).ToList() ?? new List<int>(),

                    ClientUsers = userClients
                };

                result.Add(dto);
            }

            return result;
        }
    }
}

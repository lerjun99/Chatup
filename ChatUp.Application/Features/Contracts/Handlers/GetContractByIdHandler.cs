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
    public class GetContractByIdHandler : IRequestHandler<GetContractByIdQuery, ContractDto?>
    {
        private readonly IChatDBContext _context;

        public GetContractByIdHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<ContractDto?> Handle(GetContractByIdQuery request, CancellationToken cancellationToken)
        {
            var contract = await _context.Contracts
                .Include(c => c.Projects)
                .Include(c => c.UserContracts)
                .ThenInclude(uc => uc.UserAccount)
                .Include(c => c.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contract == null)
                return null;

            // ✅ Fetch related users under the same client
            var userClients = await _context.UserClients
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

            return new ContractDto
            {
                EmailAddress = contract.Client?.Email ?? string.Empty,
                Id = contract.Id,
                Title = contract.Title,
                Description = contract.Description,
                StartDate = contract.StartDate,
                ExpirationDate = contract.ExpirationDate,
                IsTerminated = contract.IsTerminated,
                UserType = contract.UserContracts?.FirstOrDefault()?.UserType ?? 0,
                ClientId = contract.ClientId ?? 0,
                ClientName = contract.Client?.ClientName ?? string.Empty,
                ProjectTitles = contract.Projects?.Select(p => p.Title).ToList() ?? new List<string>(),
                // ✅ Convert entities to ID lists
                ProjectIds = contract.Projects?.Select(p => p.Id).ToList() ?? new List<int>(),
                UserIds = contract.UserContracts?.Select(uc => uc.UserAccountId).ToList() ?? new List<int>(),

                // ✅ Add all users under the same client
                ClientUsers = userClients
            };
        }
    }
}

using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Handlers
{
    public class GetUserClientAssignmentsByClientIdHandler
    : IRequestHandler<GetUserClientAssignmentsByClientIdQuery, List<UserClientAssignmentDto>>
    {
        private readonly IChatDBContext _context;

        public GetUserClientAssignmentsByClientIdHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<UserClientAssignmentDto>> Handle(GetUserClientAssignmentsByClientIdQuery request, CancellationToken cancellationToken)
        {
           // STEP 1: Load all assignments with related User and Client data
            var assignments = await _context.UserClientAssignments
                .Include(x => x.User)
                .Include(x => x.Client).Where(a => a.User.IsDeleted == 0)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (!assignments.Any())
                return new List<UserClientAssignmentDto>();

            // STEP 2: Load all user accounts once to prevent multiple DB calls
            var allUserAccounts = await _context.UserAccounts
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // STEP 3: Build the result list
            var result = assignments.Select(a => new UserClientAssignmentDto
            {
                Id = a.Id,
                UserId = a.UserId,
                ClientId = a.ClientId,
                UserName = a.User?.Username ?? string.Empty,
                FullName = a.User?.FullName ?? string.Empty,
                ClientName = a.Client?.ClientName ?? string.Empty,
                Location = a.Client.Location,
                UserType = a.UserType,
                // ✅ Filter only user accounts under the same ClientId
                UserAccounts = allUserAccounts
                    .Where(u => u.ClientId == a.ClientId && u.IsDeleted == 0)
                    .Select(u => new UserAccountSimpleDto
                    {
                        Username = u.Username,
                        FullName = u.FullName,
                        EmailAddress = u.EmailAddress
                        
                    })
                    .ToList()
            })
            .ToList();

            return result;
        }

    }
}

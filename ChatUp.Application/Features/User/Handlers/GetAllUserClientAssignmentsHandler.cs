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
    public class GetAllUserClientAssignmentsHandler : IRequestHandler<GetAllUserClientAssignmentsQuery, List<UserClientAssignmentDto>>
    {
        private readonly IChatDBContext _context;

        public GetAllUserClientAssignmentsHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<UserClientAssignmentDto>> Handle(GetAllUserClientAssignmentsQuery request, CancellationToken cancellationToken)
        {
            return await _context.UserClientAssignments
                .Include(x => x.User)
                .Include(x => x.Client).Where(a=>a.User.IsDeleted == 0)
                .Select(x => new UserClientAssignmentDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    ClientId = x.ClientId,
                    UserName = x.User.FullName,
                    ClientName = x.Client.ClientName
                })
                .ToListAsync(cancellationToken);
        }
    }
}

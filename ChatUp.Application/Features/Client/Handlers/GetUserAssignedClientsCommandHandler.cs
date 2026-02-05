using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Client.Commands;
using ChatUp.Application.Features.Client.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Client.Handlers
{
    public class GetUserAssignedClientsCommandHandler
        : IRequestHandler<GetUserAssignedClientsCommand, List<AssignedClientDto>>
    {
        private readonly IChatDBContext _context;

        public GetUserAssignedClientsCommandHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<AssignedClientDto>> Handle(
            GetUserAssignedClientsCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _context.UserAccounts
             .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

            if (user == null)
                return new List<AssignedClientDto>();

            bool isAdmin = user.UserType == 1;   // ⬅️ Change if needed

            IQueryable<AssignedClientDto> query;

            if (isAdmin)
            {
                // ✅ Admin: return ALL clients
                query = _context.Client
                    .Where(c => c.IsActive == 0) // optional filter
                    .Select(c => new AssignedClientDto
                    {
                        ClientId = c.Id ?? 0,
                        ClientName = c.ClientName,
                        Location = c.Location,
                        PhotoUrl = c.PhotoUrl,
                        EmailAddress = c.EmailAddress
                    });
            }
            else
            {
                // ❗ Non-admin: return assigned clients only
                query = _context.UserClientAssignments
                    .Where(x => x.UserId == request.UserId)
                    .Include(x => x.Client)
                    .Select(x => new AssignedClientDto
                    {
                        ClientId = x.ClientId ?? 0,
                        ClientName = x.Client.ClientName,
                        Location = x.Client.Location,
                        PhotoUrl = x.Client.PhotoUrl,
                        EmailAddress = x.Client.EmailAddress
                    });
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}

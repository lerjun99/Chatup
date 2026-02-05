using ChatUp.Application.Features.User.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Queries
{
    public record GetClientUsersQuery(int ClientId) : IRequest<List<UserClientDto>>;
}

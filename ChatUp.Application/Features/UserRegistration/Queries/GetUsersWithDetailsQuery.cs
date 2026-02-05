using ChatUp.Application.Features.User.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Queries
{
    // List of users query
    public class GetUsersWithDetailsQuery : IRequest<List<UserDetailsDto>>
    {
        // No parameters needed
    }
}

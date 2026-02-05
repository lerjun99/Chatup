using ChatUp.Application.Features.User.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserRegistration.Queries
{
    public class GetUserDetailsQuery : IRequest<UserDetailsDto>
    {
        public int UserId { get; set; }

        public GetUserDetailsQuery(int userId)
        {
            UserId = userId;
        }
    }
}

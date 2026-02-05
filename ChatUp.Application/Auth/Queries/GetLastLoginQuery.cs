using ChatUp.Application.Auth.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.Queries
{
    public record GetLastLoginQuery(int UserId) : IRequest<LoginInfoDto?>;
}

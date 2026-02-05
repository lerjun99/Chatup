using ChatUp.Application.Features.Projects.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Queries
{
    public class GetUserProjectsQuery : IRequest<IEnumerable<UserProjectDto>>
    {
    }
}

using ChatUp.Application.Features.Projects.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Queries
{
    public class GetUserProjectByIdQuery : IRequest<UserProjectDto>
    {
        public int UserAccountId { get; set; }
        public int ProjectId { get; set; }

        public GetUserProjectByIdQuery(int userAccountId, int projectId)
        {
            UserAccountId = userAccountId;
            ProjectId = projectId;
        }
    }
}

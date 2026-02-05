using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Projects.Commands
{
    public class DeleteUserProjectCommand : IRequest
    {
        public int UserAccountId { get; set; }
        public int ProjectId { get; set; }

        public DeleteUserProjectCommand(int userAccountId, int projectId)
        {
            UserAccountId = userAccountId;
            ProjectId = projectId;
        }
    }
}

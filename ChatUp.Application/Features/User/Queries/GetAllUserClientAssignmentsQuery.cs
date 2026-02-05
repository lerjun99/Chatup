using ChatUp.Application.Features.UserRegistration.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.User.Queries
{
    public class GetAllUserClientAssignmentsQuery : IRequest<List<UserClientAssignmentDto>> { }
}

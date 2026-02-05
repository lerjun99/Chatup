using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.Commands
{
    namespace ChatUp.Application.Features.LoginHistory.Commands
    {
        public record RecordLoginCommand(int UserId) : IRequest<Unit>;
    }
}

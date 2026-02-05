using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Commands
{
    public record DeleteApplicantCommand(int Id) : IRequest;
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.EmailOTP.Commands
{

    public record ResendOtpCommand(string Email) : IRequest<bool>;
}

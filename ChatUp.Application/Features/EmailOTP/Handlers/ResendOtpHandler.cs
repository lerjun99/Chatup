using ChatUp.Application.Features.EmailOTP.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.EmailOTP.Handlers
{
    public class ResendOtpHandler : IRequestHandler<ResendOtpCommand, bool>
    {
        private readonly IMediator _mediator;

        public ResendOtpHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<bool> Handle(ResendOtpCommand request, CancellationToken ct)
        {
            return await _mediator.Send(new SendOtpCommand(request.Email), ct);
        }
    }

}

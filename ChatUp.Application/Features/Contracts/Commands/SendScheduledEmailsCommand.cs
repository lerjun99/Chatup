using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Commands
{

    public class SendScheduledEmailsCommand : IRequest
    {
        public DateTime CurrentDate { get; set; } = DateTime.UtcNow;
    }
}

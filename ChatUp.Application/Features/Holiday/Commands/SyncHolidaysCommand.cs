using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Holiday.Commands
{
    public record SyncHolidaysCommand(string Region, int Year)
        : IRequest<Unit>;
}

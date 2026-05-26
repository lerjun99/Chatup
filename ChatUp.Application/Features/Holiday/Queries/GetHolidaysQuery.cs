using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Holiday.Queries
{
    public record GetHolidaysQuery(string Region) : IRequest<List<ChatUp.Domain.Entities.Holiday>>;
}

using ChatUp.Application.Features.TicketMessage.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Queries
{
    public record GetTicketUploadsQuery(int TicketId) : IRequest<List<TicketUploadDto>>;
}

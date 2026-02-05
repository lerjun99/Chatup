using AutoMapper;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.TicketMessage.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.TicketMessage.Handlers
{
    public class GetTicketUploadsHandler : IRequestHandler<GetTicketUploadsQuery, List<TicketUploadDto>>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;

        public GetTicketUploadsHandler(IChatDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<TicketUploadDto>> Handle(GetTicketUploadsQuery request, CancellationToken cancellationToken)
        {
            var items = await _context.TicketUploads
                .Where(u => u.TicketMessageId == request.TicketId)
                .OrderByDescending(u => u.DateUploaded)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<TicketUploadDto>>(items);
        }
    }
}

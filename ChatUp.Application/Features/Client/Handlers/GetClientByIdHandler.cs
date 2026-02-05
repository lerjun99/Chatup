using AutoMapper;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Client.DTOs;
using ChatUp.Application.Features.Client.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Client.Handlers
{
    public class GetClientByIdHandler : IRequestHandler<GetClientByIdQuery, ClientDto?>
    {
        private readonly IChatDBContext _context;
        private readonly IMapper _mapper;

        public GetClientByIdHandler(IChatDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ClientDto?> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
        {
            var client = await _context.Client.AsNoTracking()
                .Where(c => c.Id == request.Id && c.IsDeleted == 0)
                .FirstOrDefaultAsync(cancellationToken);

            return client is null ? null : _mapper.Map<ClientDto>(client);
        }
    }
}

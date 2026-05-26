using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Holiday.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Holiday.Handlers
{
    public class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, List<ChatUp.Domain.Entities.Holiday>>
    {
        private readonly IHolidayRepository _repo;

        public GetHolidaysQueryHandler(IHolidayRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ChatUp.Domain.Entities.Holiday>> Handle(GetHolidaysQuery request, CancellationToken ct)
        {
            return await _repo.GetByRegionAsync(request.Region);
        }
    }
}

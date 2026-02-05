using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.DTOs;
using ChatUp.Application.Features.UserApplicant.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class GetApplicantsHandler : IRequestHandler<GetApplicantsQuery, List<ApplicantListDto>>
    {
        private readonly IChatDBContext _context;

        public GetApplicantsHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<List<ApplicantListDto>> Handle(GetApplicantsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Applicants
                .OrderByDescending(x => x.ApplicationDate)
                .Select(x => new ApplicantListDto
                {
                    Id = x.Id!.Value,
                    ApplicantName = x.ApplicantName,
                    School = x.School,
                    Batch = x.Batch,
                    Status = x.Status,
                    ApplicationDate = x.ApplicationDate
                }).OrderByDescending(x => x.ApplicationDate)
                .ToListAsync(cancellationToken);
        }
    }
}

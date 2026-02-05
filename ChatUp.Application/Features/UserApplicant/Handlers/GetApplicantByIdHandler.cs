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
    public class GetApplicantByIdHandler : IRequestHandler<GetApplicantByIdQuery, ApplicantDetailDto>
    {
        private readonly IChatDBContext _context;

        public GetApplicantByIdHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<ApplicantDetailDto> Handle(GetApplicantByIdQuery request, CancellationToken cancellationToken)
        {
            var applicant = await _context.Applicants
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (applicant == null)
                throw new KeyNotFoundException($"Applicant with ID {request.Id} not found.");

            return new ApplicantDetailDto
            {
                Id = applicant.Id ?? 0,
                ApplicantName = applicant.ApplicantName,
                School = applicant.School,
                Batch = applicant.Batch,
                Email = applicant.Email,
                PhoneNumber = applicant.PhoneNumber,
                HomeAddress = applicant.HomeAddress,
                CvLink = applicant.CvLink,
                RequiredHours = applicant.RequiredHours,
                Status = applicant.Status.ToString(),
                Remarks = applicant.Remarks,
                Screening = applicant.Screening,
                Interview = applicant.Interview,
                AcceptanceLetter = applicant.AcceptanceLetter,
                Orientation = applicant.Orientation,
                Onboarding = applicant.Onboarding,
                IssuedCertificate = applicant.IssuedCertificate,
                SendingEmail = applicant.SendingEmail
            };
        }
    }
}

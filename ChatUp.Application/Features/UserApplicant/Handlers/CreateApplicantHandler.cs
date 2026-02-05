using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.Commands;
using ChatUp.Application.Features.UserApplicant.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class CreateApplicantHandler
       : IRequestHandler<CreateApplicantCommand, Result<int>>
    {
        private readonly IChatDBContext _context;

        public CreateApplicantHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<Result<int>> Handle(
        CreateApplicantCommand request,
        CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            bool exists = await _context.Applicants
                .AnyAsync(a => a.ApplicantName.ToLower() == dto.ApplicantName.ToLower(),
                          cancellationToken);

            if (exists)
            {
                return Result<int>.Fail(
                    $"Applicant with name '{dto.ApplicantName}' already exists."
                );
            }

            var applicant = new Applicant
            {
                ApplicantName = dto.ApplicantName,
                Batch = dto.Batch,
                School = dto.School,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                HomeAddress = dto.HomeAddress,
                CvLink = dto.CvLink,
                RequiredHours = dto.RequiredHours,
                ApplicationDate = DateTime.UtcNow,
                Status = dto.Status,
                Remarks = dto.Remarks,
                CertificateLink = string.Empty,
                Screening = false,
                Interview = false,
                AcceptanceLetter = false,
                Orientation = false,
                Onboarding = false,
                IssuedCertificate = false,
                SendingEmail = false
            };

            _context.Applicants.Add(applicant);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<int>.Ok(applicant.Id ?? 0);
        }
    }
}

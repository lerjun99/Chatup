using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class UpdateApplicantHandler : IRequestHandler<UpdateApplicantCommand, bool>
    {
        private readonly IChatDBContext _context;

        public UpdateApplicantHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateApplicantCommand request, CancellationToken cancellationToken)
        {
            var applicant = await _context.Applicants.FindAsync(request.Id);

            if (applicant == null)
                throw new KeyNotFoundException($"Applicant with ID {request.Id} not found.");

            var dto = request.Dto;

            // Update fields
            applicant.ApplicantName = dto.ApplicantName ?? applicant.ApplicantName;
            applicant.School = dto.School ?? applicant.School;
            applicant.Batch = dto.Batch ?? applicant.Batch;
            applicant.Email = dto.Email ?? applicant.Email;
            applicant.PhoneNumber = dto.PhoneNumber ?? applicant.PhoneNumber;
            applicant.HomeAddress = dto.HomeAddress ?? applicant.HomeAddress;
            applicant.CvLink = dto.CvLink ?? applicant.CvLink;
            applicant.RequiredHours = dto.RequiredHours ?? applicant.RequiredHours;

            // Optional: update status and remarks
            if (!string.IsNullOrWhiteSpace(dto.Status))
                applicant.Status = dto.Status;

            if (!string.IsNullOrWhiteSpace(dto.Remarks))
                applicant.Remarks = dto.Remarks;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating applicant: {ex.Message}");
                throw new InvalidOperationException("Failed to update applicant. Check database configuration.", ex);
            }

            return true;
        }
    }
}

using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.UserApplicant.Commands;
using ChatUp.Application.Features.UserApplicant.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Handlers
{
    public class UpdateApplicantStatusHandler
         : IRequestHandler<UpdateApplicantStatusCommand, UpdateApplicantStatusResponseDto>
    {
        private readonly IChatDBContext _context;
        private readonly IEmailService _emailService;

        public UpdateApplicantStatusHandler(
            IEmailService emailService,
            IChatDBContext context)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<UpdateApplicantStatusResponseDto> Handle(
            UpdateApplicantStatusCommand request,
            CancellationToken cancellationToken)
        {
            var applicant = await _context.Applicants.FindAsync(request.Id);

            if (applicant == null)
                throw new Exception("Applicant not found");

            if (request.Dto == null)
                throw new Exception("UpdateApplicantStatus DTO is null");

            // Normalize status
            var status = request.Dto.Status?.Trim();

            applicant.Status = status;
            applicant.Screening = request.Dto.Screening;
            applicant.Interview = request.Dto.Interview;
            applicant.AcceptanceLetter = request.Dto.AcceptanceLetter;
            applicant.Orientation = request.Dto.Orientation;
            applicant.Onboarding = request.Dto.Onboarding;
            applicant.Remarks = request.Dto.Remarks;

            await _context.SaveChangesAsync(cancellationToken);

            // 🚫 BACKOUT → update only, NO EMAIL
            if (status == "Backout")
            {
                return new UpdateApplicantStatusResponseDto
                {
                    Id = applicant.Id ?? 0,
                    ApplicantName = applicant.ApplicantName,
                    Email = applicant.Email,
                    Status = applicant.Status,
                    Screening = applicant.Screening,
                    Interview = applicant.Interview,
                    AcceptanceLetter = applicant.AcceptanceLetter,
                    Orientation = applicant.Orientation,
                    Onboarding = applicant.Onboarding,
                    Remarks = applicant.Remarks,
                    Message = "Applicant status updated (Backout)"
                };
            }
            // 📧 Send email (unchanged logic)
            //if (!string.IsNullOrEmpty(applicant.Email))
            //{
            //    string subject = string.Empty;
            //    string body = string.Empty;
            //    var scheduleInfo = !string.IsNullOrWhiteSpace(applicant.Remarks)
            //    ? $@"<p><b>Arrival Schedule:</b><br/>{applicant.Remarks}</p>"
            //    : string.Empty;
            //    var companyFooter = @"
            //    <hr />
            //    <p><b>ODECCI SOLUTIONS INC.</b><br/>
            //    UNIT 1B GD Plaza, Ilang-ilang<br/>
            //    Guiguinto, Bulacan<br/>
            //    Philippines 3015</p>

            //    <p>
            //    🌐 <a href='https://odecci.com' target='_blank'>https://odecci.com</a>
            //    </p>";
            //    switch (applicant.Status)
            //    {
            //        case "Initial Assessment":
            //            subject = "OJT Application – Initial Assessment";
            //            body = $@"
            //            <p>Dear {applicant.ApplicantName},</p>

            //            <p>As part of the onboarding process, you are required to complete the <b>Initial Assessment</b>.</p>

            //            <p>Please fill out the assessment form using the link below:</p>

            //            <p><a href='https://forms.office.com/r/rh9Aer2ShT?origin=lprLink' target='_blank'>Initial Assessment Form</a></p>

            //            {(!string.IsNullOrWhiteSpace(applicant.Remarks)
            //                                ? $@"<p><b>Additional Remarks:</b><br/>{applicant.Remarks}</p>"
            //                                : string.Empty)}

            //            <p>Thank you,<br/>
            //            <b>ODECCI Solutions Team</b></p>

            //            {companyFooter}";
            //                            break;

            //                        case "For Interview":
            //                            subject = "OJT Application – Interview Invitation";
            //                            body = $@"
            //            <p>Dear {applicant.ApplicantName},</p>

            //            <p>Congratulations! You have passed the initial screening.</p>

            //            <p>You are invited for an interview. Please arrive on time based on the schedule below:</p>

            //            {scheduleInfo}

            //            <p><b>School:</b> {applicant.School}</p>

            //            <p>Thank you,<br/>
            //            <b>ODECCI Solutions Team</b></p>

            //            {companyFooter}";
            //                            break;

            //                        case "Exam Passed":
            //                            subject = "OJT Application – Exam Passed";
            //                            body = $@"
            //            <p>Dear {applicant.ApplicantName},</p>

            //            <p>We are pleased to inform you that you have <b>passed the exam</b>.</p>

            //            <p>You will proceed to the final interview stage. Please follow the schedule below:</p>

            //            {scheduleInfo}

            //            <p>Thank you,<br/>
            //            <b>ODECCI Solutions Team</b></p>

            //            {companyFooter}";
            //                            break;

            //                        case "Final Interview":
            //                            subject = "OJT Application – Final Interview Invitation";
            //                            body = $@"
            //            <p>Dear {applicant.ApplicantName},</p>

            //            <p>You are scheduled for the <b>final interview</b>.</p>

            //            <p>Please arrive based on the schedule provided below:</p>

            //            {scheduleInfo}

            //            <p>Thank you,<br/>
            //            <b>ODECCI Solutions Team</b></p>

            //            {companyFooter}";
            //                            break;

            //                        case "Shortlisted":
            //                            subject = "OJT Application – Shortlisted for Final Review";
            //                            body = $@"
            //            <p>Hi {applicant.ApplicantName},</p>

            //            <p>We appreciate your time and interest in the Internship Program at Odecci.</p>

            //            <p>We are currently reviewing all applicants. Our final pooling and shortlisting will take place soon. All applicants will be notified of the results shortly after.</p>

            //            <p>We appreciate your patience during this process.</p>

            //            <p>Thank you,<br/>
            //            <b>ODECCI Solutions Team</b></p>

            //            {companyFooter}";
            //                            break;

            //                        case "Rejected":
            //                            subject = "OJT Application – Regret Letter";
            //                            body = $@"
            //            <p>Dear {applicant.ApplicantName},</p>

            //            <p>Thank you for your interest in our internship program and for taking the time to interview with us.</p>

            //            <p>After careful assessment, we regret to inform you that we will not be moving forward with your application at this time.</p>

            //            <p>We wish you every success in your future endeavors and encourage you to apply for future opportunities with our organization.</p>

            //            <p>Thank you,<br/>
            //            <b>ODECCI Solutions Team</b></p>

            //            {companyFooter}";
            //            break;
            //        case "Onboarding":
            //            subject = "OJT Application – Onboarding Requirements";

            //            body = $@"
            //                <p>Dear {applicant.ApplicantName},</p>

            //                <p>Greetings from <b>ODECCI Solutions Inc.</b></p>

            //                <p>
            //                    We are pleased to inform you that you have been considered for the 
            //                    <b>On-the-Job Training (OJT)</b> program under our 
            //                    <b>Internship Program - Management SOP</b>.
            //                </p>

            //                <p>
            //                    To proceed with your onboarding, we kindly request you to submit the following requirements:
            //                </p>

            //                <ol>
            //                    <li>Valid School ID</li>
            //                    <li>Memorandum of Agreement (MOA) from your school</li>
            //                    <li>Other school-specific forms or letters requiring signatories from both parties</li>
            //                </ol>

            //                <p>
            //                    <b>Please note:</b> Interns cannot start their training without submission of the 
            //                    complete requirements. All documents must be submitted to the 
            //                    <b>Office Admin / Liaison Officer</b> for verification, filing, and compliance tracking.
            //                </p>

            //                <p>
            //                    Once your requirements are verified, we will schedule your 
            //                    <b>orientation and onboarding session</b>, where you will be:
            //                </p>

            //                <ul>
            //                    <li>Introduced to the company</li>
            //                    <li>Meet the team</li>
            //                    <li>Walked through the internship roadmap</li>
            //                </ul>

            //                <p>
            //                    During this session, you will also be required to sign the following agreements:
            //                </p>

            //                <ul>
            //                    <li>Non-Disclosure Agreement (NDA) &amp; IP Assignment Agreement</li>
            //                    <li>Code of Conduct Agreement</li>
            //                </ul>

            //                {(!string.IsNullOrWhiteSpace(applicant.Remarks)
            //                                        ? $@"<p><b>Additional Remarks:</b><br/>{applicant.Remarks}</p>"
            //                                        : string.Empty)}

            //                <p>
            //                    We look forward to working with you and supporting your professional development 
            //                    during your internship.
            //                </p>

            //                <p>
            //                    Best regards,<br/>
            //                    <b>ODECCI Solutions Team</b>
            //                </p>

            //                {companyFooter}";
            //            break;
            //    }

            //    if (!string.IsNullOrEmpty(subject))
            //        await _emailService.SendEmailAsync(applicant.Email, subject, body);
            //}

            // ✅ Return DTO response
            return new UpdateApplicantStatusResponseDto
            {
                Id = applicant.Id ?? 0,
                ApplicantName = applicant.ApplicantName,
                Email = applicant.Email,
                Status = applicant.Status,
                Screening = applicant.Screening,
                Interview = applicant.Interview,
                AcceptanceLetter = applicant.AcceptanceLetter,
                Orientation = applicant.Orientation,
                Onboarding = applicant.Onboarding,
                Remarks = applicant.Remarks,
                Message = "Applicant status updated successfully"
            };
        }
    }

}



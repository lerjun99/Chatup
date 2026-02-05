using ChatUp.Application.Features.UserApplicant.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Commands
{
    public class UpdateApplicantCommand : IRequest<bool>
    {
        public int Id { get; set; }               // Applicant ID to update
        public UpdateApplicantInfoDto Dto { get; set; }  // DTO with new values

        public UpdateApplicantCommand(int id, UpdateApplicantInfoDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }
}

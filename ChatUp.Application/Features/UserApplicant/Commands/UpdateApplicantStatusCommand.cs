using ChatUp.Application.Features.UserApplicant.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Commands
{
    public record UpdateApplicantStatusCommand(
          int Id,
          UpdateApplicantStatusDto Dto
      ) : IRequest<UpdateApplicantStatusResponseDto>;
}

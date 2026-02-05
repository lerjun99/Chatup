using ChatUp.Application.Features.UserApplicant.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Queries
{
    public record GetApplicantsQuery : IRequest<List<ApplicantListDto>>;
}

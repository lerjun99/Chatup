using ChatUp.Application.Features.UserApplicant.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.UserApplicant.Queries
{
    public class GetApplicantByIdQuery : IRequest<ApplicantDetailDto>
    {
        public int Id { get; }

        public GetApplicantByIdQuery(int id)
        {
            Id = id;
        }
    }
}

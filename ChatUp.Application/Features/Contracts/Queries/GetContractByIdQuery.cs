using ChatUp.Application.Features.Contracts.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Queries
{
    public class GetContractByIdQuery : IRequest<ContractDto?>
    {
        public int Id { get; set; }

        public GetContractByIdQuery(int id)
        {
            Id = id;
        }
    }
}

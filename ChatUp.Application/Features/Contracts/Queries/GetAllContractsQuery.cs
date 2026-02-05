using ChatUp.Application.Features.Contracts.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Queries
{
    public class GetAllContractsQuery : IRequest<List<ContractDto>>
    {
    }
}

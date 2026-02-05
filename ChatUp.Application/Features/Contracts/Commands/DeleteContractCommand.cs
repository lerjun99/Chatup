using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Commands
{
    public class DeleteContractCommand : IRequest<Unit>
    {
        public int Id { get; set; }
    }
}

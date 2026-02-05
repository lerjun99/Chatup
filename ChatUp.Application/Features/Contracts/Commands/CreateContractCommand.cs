using ChatUp.Application.Features.Contracts.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Commands
{
    public class CreateContractCommand : IRequest<ContractDto>
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public List<int> ProjectIds { get; set; } = new();
        public List<int> UserIds { get; set; } = new();
        public int? UserType { get; set; } 
        public int ClientId { get; set; } 
    }
}

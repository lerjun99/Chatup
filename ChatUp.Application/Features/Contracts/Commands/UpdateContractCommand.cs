using ChatUp.Application.Features.Contracts.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Commands
{
    public class UpdateContractCommand : IRequest<ContractDto>
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public List<int> ProjectIds { get; set; } = new();
        public List<int> UserIds { get; set; } = new();
        public int? UserType { get; set; }

        public UpdateContractCommand() { }

        public UpdateContractCommand(int id, int ClientId , string title, string description, DateTime? expirationDate, List<int> projectIds, List<int> userIds, int usertype)
        {
            Id = id;
            ClientId =  ClientId;
            Title = title;
            Description = description;
            ExpirationDate = expirationDate;
            ProjectIds = projectIds ?? new();
            UserIds = userIds ?? new();
            UserType = usertype;
        }
    }
}

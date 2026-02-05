using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Contracts.Commands;
using ChatUp.Application.Features.Contracts.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract = ChatUp.Domain.Entities.Contract;
namespace ChatUp.Application.Features.Contracts.Handlers
{
    public class CreateContractHandler : IRequestHandler<CreateContractCommand, ContractDto>
    {
        private readonly IChatDBContext _context;

        public CreateContractHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<ContractDto> Handle(CreateContractCommand request, CancellationToken cancellationToken)
        {

            var contract = new Contract
            {
                Title = request.Title,
                Description = request.Description,
                ClientId = request.ClientId,
                ExpirationDate = request.ExpirationDate,
                DateCreated = DateTime.UtcNow
            };
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync(cancellationToken);


            // attach projects
            if (request.ProjectIds?.Any() == true)
            {
                var projects = _context.Projects.Where(p => request.ProjectIds.Contains(p.Id)).ToList();
                foreach (var p in projects) { p.ContractId = contract.Id; }
            }


            // attach users (many-to-many via junction)
            // 🔹 Create UserProjects (user ↔ project)
            if (request.UserIds?.Any() == true && request.ProjectIds?.Any() == true)
            {
                foreach (var projectId in request.ProjectIds)
                {
                    foreach (var userId in request.UserIds)
                    {
                        _context.UserProjects.Add(new UserProject
                        {
                            UserAccountId = userId,
                            ProjectId = projectId,
                            UserType = request.UserType ?? 0
                        });
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new ContractDto
            {
                Id = contract.Id,
                Title = contract.Title,
                Description = contract.Description,
                StartDate = contract.StartDate,
                ExpirationDate = contract.ExpirationDate
            };
        }
    }
 }
    


using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Contracts.Commands;
using ChatUp.Application.Features.Contracts.DTOs;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Contracts.Handlers
{
    public class UpdateContractHandler : IRequestHandler<UpdateContractCommand, ContractDto>
    {
        private readonly IChatDBContext _context;

        public UpdateContractHandler(IChatDBContext context)
        {
            _context = context;
        }

        public async Task<ContractDto> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
        {
            var contract = await _context.Contracts
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contract == null)
                throw new KeyNotFoundException($"Contract with ID {request.Id} not found.");

            // 🔹 Update base properties
            contract.Title = request.Title;
            contract.Description = request.Description;
            contract.ExpirationDate = request.ExpirationDate;
            contract.DateUpdated = DateTime.UtcNow;
            contract.ClientId = request.ClientId;

            // 🔹 Update Project references
            if (request.ProjectIds?.Any() == true)
            {
                var projects = _context.Projects.Where(p => p.ContractId == contract.Id).ToList();
                foreach (var p in projects)
                    p.ContractId = null; // detach existing

                var newProjects = _context.Projects.Where(p => request.ProjectIds.Contains(p.Id)).ToList();
                foreach (var p in newProjects)
                    p.ContractId = contract.Id;
            }

            // 🔹 Update Users (junction table)
            var existingUserContracts = _context.UserContracts.Where(uc => uc.ContractId == contract.Id).ToList();
            _context.UserContracts.RemoveRange(existingUserContracts);

            if (request.UserIds?.Any() == true)
            {
                foreach (var uid in request.UserIds)
                {
                    _context.UserContracts.Add(new UserContract
                    {
                        ContractId = contract.Id,
                        UserAccountId = uid,
                        UserType = request.UserType ?? 0
                    });
                }
            }
            // 🔹 Update UserProjects (user ↔ project)
            var existingUserProjects = await _context.UserProjects
                .Where(up => request.ProjectIds.Contains(up.ProjectId))
                .ToListAsync(cancellationToken);

            _context.UserProjects.RemoveRange(existingUserProjects);

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
                ExpirationDate = contract.ExpirationDate,
                ClientId = contract.ClientId ?? 0,
                UserType = request.UserType ?? 0
            };
        }
    }
}

using ChatUp.Application.Common.Helpers;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.Commands;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using DocumentFormat.OpenXml.InkML;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class CreateTicketHandler : IRequestHandler<CreateTicketCommand, TicketCreatedDto>
{
    private readonly ITicketRepository _repo;
    private readonly IProjectRepository _projectRepo;
    public CreateTicketHandler(ITicketRepository repo, IProjectRepository projectRepo)
    {
        _repo = repo;
       _projectRepo = projectRepo;
    }
    public async Task<TicketCreatedDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = new Ticket
            {
                DateReceived = request.DateReceived,
                IssueTitle = request.IssueTitle,
                Description = request.Concern,

                // ✅ Use foreign key IDs
                RequestedById = request.RequestedById,
                ClientId = request.ClientId,
                SupportedById = request.SupportedById,
                ProjectId = request.ProjectId,
                Priority = request.Priority,
                Status = Enum.TryParse<TicketStatus>(request.Status, out var status)
                         ? status
                         : TicketStatus.Open,
                DueDate = SlaHelper.CalculateDueDate(request.Priority),
                IsCase = request.IsCase
            };

        await _repo.AddAsync(ticket, cancellationToken);

            string projectName = string.Empty;
            if (ticket.ProjectId.HasValue)
            {
                var project = await _projectRepo.GetByIdAsync(ticket.ProjectId.Value, cancellationToken);
                if (project != null)
                    projectName = project.Title;
            }
            var assignedDev = await _projectRepo.GetAssignedDeveloperAsync(
                ticket.ProjectId!.Value,
                cancellationToken
            );

            string developerEmail = assignedDev?.EmailAddress ?? "";
            string developerName = assignedDev?.FullName ?? "";
            return new TicketCreatedDto
            {
                Id = ticket.Id,
                TicketNo = ticket.TicketNo,
                IssueTitle = ticket.IssueTitle,
                Concern = ticket.Concern,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority,
                ClientId = ticket.ClientId ?? 0,
                ClientName = string.Empty,
                ProjectId = ticket.ProjectId ?? 0,
                ProjectName = projectName,
                DueDate = ticket.DueDate ?? DateTime.MinValue,
                IsCase = ticket.IsCase, // ✅ Map IsCase
                CaseNo = ticket.IsCase ? ticket.TicketNo : string.Empty, // optional
                    DeveloperEmail = developerEmail,
                    DeveloperName = developerName
                };
            }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"Database error while creating ticket upload: {dbEx.Message}");
            if (dbEx.InnerException != null)
                Console.WriteLine("INNER EXCEPTION: " + dbEx.InnerException.Message);

            throw;
        }
    }
}

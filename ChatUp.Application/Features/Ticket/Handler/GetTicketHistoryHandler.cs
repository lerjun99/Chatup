using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Application.Features.Ticket.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Features.Ticket.Handler
{
    public class GetTicketHistoryHandler : IRequestHandler<GetTicketHistoryQuery, List<TicketHistoryDto>>
    {
        private readonly IChatDBContext _db;
        public GetTicketHistoryHandler(IChatDBContext db) => _db = db;

        public async Task<List<TicketHistoryDto>> Handle(GetTicketHistoryQuery request, CancellationToken ct)
        {
            var query =
          from h in _db.TicketHistories

          join u in _db.UserAccounts
              on h.UpdatedBy equals u.Id into userJoin
          from user in userJoin.DefaultIfEmpty()

              // 🔹 LEFT JOIN OLD PROJECT
          join op in _db.Projects
              on h.OldProjectId equals op.Id into oldProjectJoin
          from oldProject in oldProjectJoin.DefaultIfEmpty()

              // 🔹 LEFT JOIN NEW PROJECT
          join np in _db.Projects
              on h.NewProjectId equals np.Id into newProjectJoin
          from newProject in newProjectJoin.DefaultIfEmpty()

          where h.TicketId == request.TicketId
          orderby h.UpdatedAt descending

          select new TicketHistoryDto
          {
              Id = h.Id,
              TicketId = h.TicketId,

              // Status
              OldStatus = h.OldStatus.ToString(),
              NewStatus = h.NewStatus.ToString(),

              // Priority already exists in DB (if needed later)

              // 🔹 PROJECT IDS
              OldProjectId = h.OldProjectId,
              NewProjectId = h.NewProjectId,

              // 🔹 PROJECT NAMES
              OldProjectName = oldProject != null ? oldProject.Title : null,
              NewProjectName = newProject != null ? newProject.Title : null,

              UpdatedById = h.UpdatedBy,
              UpdatedByName = user != null ? user.FullName : "System",
              UpdatedAt = h.UpdatedAt,
              Remarks = h.Remarks
          };

            return await query.ToListAsync(ct);
        }
    }
}

using ChatUp.Application.Auth.Commands.ChatUp.Application.Features.LoginHistory.Commands;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Auth.Handlers
{
    public class RecordLoginCommandHandler : IRequestHandler<RecordLoginCommand, Unit>
    {
        private readonly ILoginHistoryRepository _repo;

        public RecordLoginCommandHandler(ILoginHistoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(RecordLoginCommand request, CancellationToken cancellationToken)
        {
            var phZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phZone);
            var history = new LoginHistory
            {
                UserId = request.UserId,
                LoginTime = localNow
            };
            await _repo.AddAsync(history, cancellationToken);
            return Unit.Value;
        }
    }
}

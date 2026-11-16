using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ChatUp.Application.Features.AutoReply
{
    internal static class AutoReplyAccess
    {
        public const int Admin = 1;
        public const int Support = 2;

        /// <summary>The caller's type and, for support agents, the clients they cover.</summary>
        public static async Task<(int? UserType, HashSet<int> ClientIds)> GetScopeAsync(
            IChatDBContext db, int userId, CancellationToken ct)
        {
            var userType = await db.UserAccounts
                .AsNoTracking()
                .Where(u => u.Id == userId && u.IsDeleted == 0)
                .Select(u => u.UserType)
                .FirstOrDefaultAsync(ct);

            if (userType != Support)
                return (userType, new HashSet<int>());

            var clientIds = await db.UserClientAssignments
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.ClientId.HasValue)
                .Select(a => a.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);

            return (userType, clientIds.ToHashSet());
        }

        /// <summary>Admins edit every row; support agents only the rows of clients they cover.</summary>
        public static bool CanEdit(int? userType, HashSet<int> clientIds, int? clientId) =>
            userType == Admin || (userType == Support && clientId.HasValue && clientIds.Contains(clientId.Value));

        public static string NormalizeTime(string? value) =>
            TimeSpan.TryParse(value, out var t) && t >= TimeSpan.Zero && t < TimeSpan.FromDays(1)
                ? t.ToString(@"hh\:mm")
                : "00:00";

        public static string? Trimmed(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public class GetAutoRepliesHandler : IRequestHandler<GetAutoRepliesQuery, List<AutoReplyDto>>
    {
        private readonly IChatDBContext _db;
        private readonly IConfiguration _config;

        public GetAutoRepliesHandler(IChatDBContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<List<AutoReplyDto>> Handle(GetAutoRepliesQuery request, CancellationToken ct)
        {
            var (userType, clientIds) = await AutoReplyAccess.GetScopeAsync(_db, request.UserId, ct);

            if (userType is not (AutoReplyAccess.Admin or AutoReplyAccess.Support))
                return new List<AutoReplyDto>();

            var query = _db.ChatAutoReplies.AsNoTracking();

            if (userType == AutoReplyAccess.Support)
                query = query.Where(x => x.ClientId == null || clientIds.Contains(x.ClientId.Value));

            var rows = await query
                .Select(x => new
                {
                    x.Id,
                    x.ClientId,
                    ClientName = x.Client != null ? x.Client.ClientName : null,
                    x.IsEnabled,
                    x.InstantMessage,
                    x.StartTime,
                    x.EndTime,
                    x.ActiveDays,
                    x.ScheduledMessage,
                    x.DateUpdated,
                    UpdatedByName = _db.UserAccounts
                        .Where(u => u.Id == x.UpdatedBy)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            var now = ChatReplyClock.LocalNow(_config["ChatTimeZone"]);

            return rows
                .OrderBy(x => x.ClientId.HasValue)
                .ThenBy(x => x.ClientName)
                .Select(x => new AutoReplyDto
                {
                    Id = x.Id,
                    ClientId = x.ClientId,
                    ClientName = x.ClientName,
                    IsEnabled = x.IsEnabled,
                    InstantMessage = x.InstantMessage,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    ActiveDays = ChatReplyClock.ParseDays(x.ActiveDays).Select(d => (int)d).OrderBy(d => d).ToList(),
                    ScheduledMessage = x.ScheduledMessage,
                    DateUpdated = x.DateUpdated,
                    UpdatedByName = x.UpdatedByName,
                    IsWindowActiveNow = x.IsEnabled && ChatReplyClock.IsWithinWindow(x.StartTime, x.EndTime, x.ActiveDays, now)
                })
                .ToList();
        }
    }

    public class SaveAutoReplyHandler : IRequestHandler<SaveAutoReplyCommand, AutoReplyDto>
    {
        private readonly IChatDBContext _db;
        private readonly IConfiguration _config;

        public SaveAutoReplyHandler(IChatDBContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AutoReplyDto> Handle(SaveAutoReplyCommand request, CancellationToken ct)
        {
            var data = request.Data ?? throw new InvalidOperationException("No auto-reply settings were sent.");
            var (userType, clientIds) = await AutoReplyAccess.GetScopeAsync(_db, request.UserId, ct);

            if (!AutoReplyAccess.CanEdit(userType, clientIds, data.ClientId))
                throw new UnauthorizedAccessException(data.ClientId.HasValue
                    ? "You can only change the auto reply of clients you support."
                    : "Only an administrator can change the default auto reply.");

            // One row per client (and one default row): saving again updates it
            var entity = data.Id > 0
                ? await _db.ChatAutoReplies.FirstOrDefaultAsync(x => x.Id == data.Id, ct)
                : await _db.ChatAutoReplies.FirstOrDefaultAsync(x => x.ClientId == data.ClientId, ct);

            if (entity == null)
            {
                entity = new ChatAutoReply { ClientId = data.ClientId };
                _db.ChatAutoReplies.Add(entity);
            }
            else if (entity.ClientId != data.ClientId)
            {
                throw new InvalidOperationException("These settings belong to a different client.");
            }

            entity.IsEnabled = data.IsEnabled;
            entity.InstantMessage = AutoReplyAccess.Trimmed(data.InstantMessage);
            entity.ScheduledMessage = AutoReplyAccess.Trimmed(data.ScheduledMessage);
            entity.StartTime = AutoReplyAccess.NormalizeTime(data.StartTime);
            entity.EndTime = AutoReplyAccess.NormalizeTime(data.EndTime);
            entity.ActiveDays = string.Join(',', (data.ActiveDays ?? new()).Where(d => d is >= 0 and <= 6).Distinct().OrderBy(d => d));
            entity.UpdatedBy = request.UserId;
            entity.DateUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var now = ChatReplyClock.LocalNow(_config["ChatTimeZone"]);

            return new AutoReplyDto
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                ClientName = data.ClientName,
                IsEnabled = entity.IsEnabled,
                InstantMessage = entity.InstantMessage,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                ActiveDays = ChatReplyClock.ParseDays(entity.ActiveDays).Select(d => (int)d).OrderBy(d => d).ToList(),
                ScheduledMessage = entity.ScheduledMessage,
                DateUpdated = entity.DateUpdated,
                IsWindowActiveNow = entity.IsEnabled && ChatReplyClock.IsWithinWindow(entity.StartTime, entity.EndTime, entity.ActiveDays, now)
            };
        }
    }

    public class DeleteAutoReplyHandler : IRequestHandler<DeleteAutoReplyCommand, bool>
    {
        private readonly IChatDBContext _db;

        public DeleteAutoReplyHandler(IChatDBContext db) => _db = db;

        public async Task<bool> Handle(DeleteAutoReplyCommand request, CancellationToken ct)
        {
            var entity = await _db.ChatAutoReplies.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (entity == null)
                return false;

            var (userType, clientIds) = await AutoReplyAccess.GetScopeAsync(_db, request.UserId, ct);
            if (!AutoReplyAccess.CanEdit(userType, clientIds, entity.ClientId))
                throw new UnauthorizedAccessException("You are not allowed to remove these auto-reply settings.");

            _db.ChatAutoReplies.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}

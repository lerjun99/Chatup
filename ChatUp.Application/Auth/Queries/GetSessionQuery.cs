using ChatUp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatUp.Application.Auth.Queries
{
    /// <summary>
    /// Is this session token still signed in? A token stops being valid when the user logs out (the token is cleared),
    /// signs in somewhere else (a new token replaces it), or the account is deactivated or deleted.
    /// </summary>
    public record GetSessionQuery(string Token) : IRequest<SessionInfoDto>;

    public sealed class SessionInfoDto
    {
        public bool Active { get; set; }
        public int? UserId { get; set; }
        public int? UserType { get; set; }
        public string? Username { get; set; }
        public int? ClientId { get; set; }

        public static SessionInfoDto Inactive => new() { Active = false };
    }

    public sealed class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, SessionInfoDto>
    {
        private readonly IChatDBContext _context;

        public GetSessionQueryHandler(IChatDBContext context) => _context = context;

        public async Task<SessionInfoDto> Handle(GetSessionQuery request, CancellationToken cancellationToken)
        {
            var token = request.Token?.Trim();
            if (string.IsNullOrEmpty(token))
                return SessionInfoDto.Inactive;

            // Not cached on purpose: a logout must take effect on the very next page
            var user = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => u.JWToken == token
                            && u.isLoggedIn == 1
                            && (u.IsActive ?? 0) == 1      // same rules as LoginHandler
                            && (u.IsDeleted ?? 0) != 1)
                .Select(u => new { u.Id, u.UserType, u.Username, u.ClientId })
                .FirstOrDefaultAsync(cancellationToken);

            if (user?.Id == null)
                return SessionInfoDto.Inactive;

            return new SessionInfoDto
            {
                Active = true,
                UserId = user.Id,
                UserType = user.UserType,
                Username = user.Username,
                ClientId = user.ClientId
            };
        }
    }
}

using ChatUp.Domain.Entities;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(UserAccount user);
    }
}

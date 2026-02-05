using Blazored.SessionStorage;
using ChatUp.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ISessionStorageService _sessionStorage;

    public CustomAuthStateProvider(ISessionStorageService sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await SafeGetItemAsync("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var identity = CreateIdentityFromToken(token);
            var claims = identity.Claims.ToList();

            var userRole = await SafeGetItemAsync("UserType");
            if (!string.IsNullOrEmpty(userRole) && !claims.Any(c => c.Type == ClaimTypes.Role))
                claims.Add(new Claim(ClaimTypes.Role, userRole));

            var userId = await SafeGetItemAsync("UserId");
            if (!string.IsNullOrEmpty(userId) && !claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            var newIdentity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(newIdentity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task NotifyUserAuthentication(string token, UserAccount user)
    {
        await _sessionStorage.SetItemAsync("AuthToken", token);
        await _sessionStorage.SetItemAsync("UserId", (user.Id ?? 0).ToString());  // ✅ Fixed here
        await _sessionStorage.SetItemAsync("UserType", user.UserType ?? 0);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, (user.Id ?? 0).ToString()),
            new Claim(ClaimTypes.Name, user.Username ?? ""),
            new Claim(ClaimTypes.Role, user.UserType.ToString() ?? "")
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task NotifyUserLogout()
    {
        await _sessionStorage.RemoveItemAsync("AuthToken");
        await _sessionStorage.RemoveItemAsync("UserId");
        await _sessionStorage.RemoveItemAsync("UserType");

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    private ClaimsIdentity CreateIdentityFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var claims = jwt.Claims.Select(claim =>
        {
            if (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                return new Claim(ClaimTypes.Role, claim.Value);

            return claim;
        });

        return new ClaimsIdentity(claims, "jwt");
    }

    private async Task<string?> SafeGetItemAsync(string key)
    {
        try
        {
            return await _sessionStorage.GetItemAsync<string>(key);
        }
        catch
        {
            return null;
        }
    }
}

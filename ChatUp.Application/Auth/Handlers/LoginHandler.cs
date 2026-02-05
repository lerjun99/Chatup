using BCrypt.Net;
using ChatUp.Application.Auth.Commands;
using ChatUp.Application.Auth.DTOs;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
namespace ChatUp.Application.Auth.Handlers;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto<LoginResponseDto>>
{
    private readonly IChatDBContext _context;
    private readonly IJwtTokenService _jwtService;
    private readonly ILogger<LoginHandler> _logger;
    private readonly ICryptography _crypto;
    private readonly IUserStatusNotifier _statusNotifier;
    private readonly IClientContext _clientContext;
    private readonly IPublicIpService _publicIpService;
    public LoginHandler(IChatDBContext context, IJwtTokenService jwtService, ILogger<LoginHandler> logger, ICryptography crypto, IUserStatusNotifier statusNotifier, IClientContext clientContext, IPublicIpService publicIpService)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _crypto = crypto;
        _statusNotifier = statusNotifier;
        _clientContext = clientContext;
        _publicIpService = publicIpService;
    }

    public async Task<AuthResponseDto<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
       
        var result = new AuthResponseDto<LoginResponseDto>();
        try
        {
            var user = await _context.UserAccounts
                 .Include(u => u.Uploads)
                .FirstOrDefaultAsync(u =>
                    u.Username == request.Username &&
                    (u.IsActive ?? 0) == 1 &&
                    (u.IsDeleted ?? 0) != 1, cancellationToken);

            if (user == null ||!string.Equals(user.Username, request.Username, StringComparison.Ordinal))
            {
                result.BaseResult = new BaseResult
                {
                    Code = "500",
                    Msg = "No Data Found",
                    MsgCode = "Error"
                };
                return result;
            }

            // Check account status
            string userStatus = user.Status?.ToString() ?? "";
            switch (userStatus)
            {
   

                case "1":
                    result.BaseResult = new BaseResult
                    {
                        Code = "200",
                        Msg = "Your account is unverified. Please contact administrator.",
                        MsgCode = "Error"
                    };
                    return result;

                case "3":
                    result.BaseResult = new BaseResult
                    {
                        Code = "200",
                        Msg = "Your account has been blocked. Please contact administrator.",
                        MsgCode = "Error"
                    };
                    return result;
            }

            // Validate password

            //var encryptedPassword = _crypto.Encrypt(request.Password);
            //string decryptedPass = _crypto.Decrypt(encryptedPassword);
            //
            bool isPasswordValid = false;
            if (user.Password.StartsWith("$2"))
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            }
            else
            {
                // Otherwise, assume it is encrypted with your custom ICryptography
          
                var encryptedPassword = _crypto.Encrypt(request.Password);
                string decryptedPass = _crypto.Decrypt(encryptedPassword);

                // Compare decrypted password with stored one
                isPasswordValid = decryptedPass == _crypto.Decrypt(user.Password);
            }
            if (!isPasswordValid)
            {
                // Handle login attempts
                var attempts = await _context.Attempts.FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);
                if (attempts != null)
                {
                    attempts.AttemptCount = (attempts.AttemptCount ?? 0) + 1;
                    if (attempts.AttemptCount > 5)
                    {
                        result.BaseResult = new BaseResult
                        {
                            Code = "500",
                            Msg = "User LogIn Attempts Exceeded. Please contact admin",
                            MsgCode = "Error"
                        };
                        return result;
                    }
                }
                else
                {
                    _context.Attempts.Add(new LoginAttempt
                    {
                        UserId = user.Id ?? 0,
                        AttemptCount = 1
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
                await _statusNotifier.NotifyStatusChanged(user.Id ?? 0, true);
                result.BaseResult = new BaseResult
                {
                    Code = "500",
                    Msg = "Invalid LogIn",
                    MsgCode = "Error"
                };
                return result;
            }

            // ✅ Password correct → Generate JWT
            var token = _jwtService.GenerateToken(user);

            user.JWToken = token;
            user.isLoggedIn = 1;
            user.DateUpdated = DateTime.Now;
            var phZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phZone);
            var ipAddress = _clientContext.IpAddress ?? "Unknown";
            var publicIp = await _publicIpService.GetPublicIpAsync();
            var userAgent = _clientContext.UserAgent;
            var loginHistory = new LoginHistory
            {
                UserId = user.Id ?? 0,
                LoginTime = localNow,
                IpAddress = publicIp,
                UserAgent = userAgent
            };
            _context.LoginHistories.Add(loginHistory);
            await _context.SaveChangesAsync(cancellationToken);
            await _statusNotifier.NotifyStatusChanged(user.Id ?? 1, true);

            result.BaseResult = new BaseResult
            {
                Code = "200",
                Status = "Success",
                Msg = "Login successful",
                MsgCode = "Ok",
                firstLogin = user.IsFirstLogIn
            };

            result.Data = new LoginResponseDto
            {
                Id = user.Id ?? 0,
                Username = user.Username,
                UserType = user.UserType ?? 0,
                FullName = user.FullName,
                Token = token,
                ClientId=user.ClientId ?? 0,
                AvatarUrl = user.Uploads != null && user.Uploads.Any()
                        ? user.Uploads.First().Base64Content
                        : "images/default.png"
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("No Data Found" + ", Log In Data: " + JsonConvert.SerializeObject(result) + ", paramaters: " + JsonConvert.SerializeObject(result));

            result.BaseResult = new BaseResult
            {
                Code = "500",
                Msg = $"Exception: {ex.Message}",
                MsgCode = "Error"
            };
            return result;
        }
     }
}

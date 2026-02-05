using ChatUp.Application.Features.EmailOTP.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;

namespace ChatUp.Services
{
    public class PasswordRecoveryService
    {
        private readonly HttpClient _http;

        public PasswordRecoveryService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Send OTP to user email
        /// </summary>
        public async Task<bool> SendOtpAsync(SendOtpCommand command)
        {
            var response = await _http.PostAsJsonAsync($"{AppConfig.ChatUrl}api/password-recovery/send-otp", command);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Resend OTP to user email
        /// </summary>
        public async Task<bool> ResendOtpAsync(ResendOtpCommand command)
        {
            var response = await _http.PostAsJsonAsync($"{AppConfig.ChatUrl}api/password-recovery/resend-otp", command);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Verify OTP entered by user
        /// </summary>
        public async Task<bool> VerifyOtpAsync(VerifyOtpCommand command)
        {
            var response = await _http.PostAsJsonAsync($"{AppConfig.ChatUrl}api/password-recovery/verify-otp", command);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Change the password using user ID and new password
        /// </summary>
        public async Task<bool> ChangePasswordAsync(ChangeLogPasswordCommand model)
        {
            var response = await _http.PostAsJsonAsync($"{AppConfig.ChatUrl}api/password-recovery/change-password", model);
            return response.IsSuccessStatusCode;
        }
    }
}

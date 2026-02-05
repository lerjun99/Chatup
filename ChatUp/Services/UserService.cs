using ChatUp.Application.Features.Client.DTOs;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Application.Features.UserRegistration.Commands;
using ChatUp.Application.Features.UserRegistration.DTOs;

namespace ChatUp.Services
{
    public class UserService
    {
        private readonly HttpClient _http;

        public UserService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<UserAccountDto>> GetAllAsync()
        {
            var url = $"{AppConfig.ChatUrl}User/GetUsers";
            return await _http.GetFromJsonAsync<List<UserAccountDto>>(url) ?? new();
        }

        public async Task<List<ClientDto>> LoadClientsAsync()
        {
            var url = $"{AppConfig.ChatUrl}Client/GetAll";
            return await _http.GetFromJsonAsync<List<ClientDto>>(url) ?? new();
        }
        public async Task<List<UserDetailsDto>> GetAllUserAsync()
        {
            var url = $"{AppConfig.ChatUrl}User/GetAllUsers";
            return await _http.GetFromJsonAsync<List<UserDetailsDto>>(url) ?? new();
        }
        // 🔹 Get user by ID
        public async Task<UserAccountDto?> GetByIdAsync(int id)
        {
            var url = $"{AppConfig.ChatUrl}User/GetUserById/{id}";
            return await _http.GetFromJsonAsync<UserAccountDto>(url);
        }

        // 🔹 Register (Create User)
        public async Task<UserAccountDto?> RegisterAsync(CreateUserCommand command)
        {
            var url = $"{AppConfig.ChatUrl}User/Register/create";
            var response = await _http.PostAsJsonAsync(url, command);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to register user: {msg}");
            }

            return await response.Content.ReadFromJsonAsync<UserAccountDto>();
        }

        // 🔹 Update user info
        public async Task<UserAccountDto?> UpdateAsync(UpdateUserCommand command)
        {
            var url = $"{AppConfig.ChatUrl}User/UpdateUserInfo/update";
            var response = await _http.PutAsJsonAsync(url, command);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to update user: {msg}");
            }

            return await response.Content.ReadFromJsonAsync<UserAccountDto>();
        }

        // 🔹 Delete user
        public async Task<bool> DeleteAsync(int id)
        {
            var url = $"{AppConfig.ChatUrl}User/delete/{id}";
            var response = await _http.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }

        // 🔹 Change Password
        public async Task<bool> ChangePasswordAsync(ChangePasswordDto  command)
        {
            var url = $"{AppConfig.ChatUrl}User/change-password";
            var response = await _http.PostAsJsonAsync(url, command);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Password change failed: {msg}");
            }

            return true;
        }
    }
}

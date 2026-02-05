using ChatUp.Application.Features.UserRegistration.DTOs;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace ChatUp.Services
{
    public class UserClientAssignmentService
    {
        private readonly HttpClient _http;

        public UserClientAssignmentService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<UserClientAssignmentDto>> GetAllAsync()
        {
            var url = $"{AppConfig.ChatUrl}User/GetAll";
            return await _http.GetFromJsonAsync<List<UserClientAssignmentDto>>(url) ?? new();
        }

        public async Task<UserClientAssignmentDto?> GetByIdAsync(int id)
        {
            var url = $"{AppConfig.ChatUrl}User/GetUsersByClient/{id}";
            return await _http.GetFromJsonAsync<UserClientAssignmentDto>(url);
        }

        public async Task<UserClientAssignmentDto?> CreateAsync(UserClientAssignmentDto model)
        {
            var url = $"{AppConfig.ChatUrl}User/Create";
            var response = await _http.PostAsJsonAsync(url, model);
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(msg);
            }
            return await response.Content.ReadFromJsonAsync<UserClientAssignmentDto>();
        }

        public async Task<UserClientAssignmentDto?> UpdateAsync(UserClientAssignmentDto model)
        {
            var url = $"{AppConfig.ChatUrl}User/Update/{model.Id}";
            var response = await _http.PutAsJsonAsync(url, model);
            return await response.Content.ReadFromJsonAsync<UserClientAssignmentDto>();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var url = $"{AppConfig.ChatUrl}User/Delete/{id}";
            var response = await _http.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}

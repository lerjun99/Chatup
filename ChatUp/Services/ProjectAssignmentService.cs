using ChatUp.Application.Features.Projects.DTOs;
using ChatUp.Application.Features.UserRegistration.DTOs;

namespace ChatUp.Services
{
    public class ProjectAssignmentService
    {
        private readonly HttpClient _http;

        public ProjectAssignmentService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ProjectDto>> GetAllAsync()
        {
            var endpoint = $"{AppConfig.ChatUrl}Projects/Get";
            return await _http.GetFromJsonAsync<List<ProjectDto>>(endpoint) ?? new();
        }

        public async Task CreateAsync(UserProjectAssignmentDto dto)
        {
           
            // Send to /Projects/AssignUserToProject
            var endpoint = $"{AppConfig.ChatUrl}Projects/AssignUserToProject";
            var response = await _http.PostAsJsonAsync(endpoint, dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(errorContent);
            }
        }

        public async Task UpdateAsync(UserProjectAssignmentDto dto)
        {
            var endpoint = $"{AppConfig.ChatUrl}Projects/UpdateUserProjectAssignment/Update";
            var response = await _http.PutAsJsonAsync(endpoint, dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(errorContent);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var endpoint = $"{AppConfig.ChatUrl}Projects/DeleteAssignment/Assignment/{id}";
            var response = await _http.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
    }
}

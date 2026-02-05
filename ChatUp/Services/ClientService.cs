using ChatUp.Application.Features.Client.DTOs;
using ChatUp.Application.Features.Projects.DTOs;
using System.Text.Json;

namespace ChatUp.Services
{
    public class ClientService
    {
        private readonly HttpClient _http;
        public ClientService(HttpClient http) => _http = http;

        public async Task<List<ClientDto>> GetAssignedClientsAsync(int userId)
        {
            var url = $"Client/GetAssignedClients/assigned/{userId}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<ClientDto>();

            var rawJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ClientDto>>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ClientDto>();
        }

        public async Task<List<ProjectDto>> GetProjectsByClientAsync(int clientId)
        {
            var url = $"Projects/GetByClient/{clientId}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<ProjectDto>();

            var rawJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ProjectDto>>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ProjectDto>();
        }
    }

}

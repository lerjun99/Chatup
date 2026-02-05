using ChatUp.Application.Features.UserApplicant.DTOs;
using System.Net.Http.Json;

namespace ChatUp.Services
{
    public class ApplicantApiService
    {
        private readonly HttpClient _http;

        public ApplicantApiService(HttpClient http)
        {
            _http = http;
        }
        public async Task<ApplicantDashboardDto> GetDashboardAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = string.Empty;

            if (from.HasValue)
                query += $"from={from:yyyy-MM-dd}";

            if (to.HasValue)
                query += $"{(query.Length > 0 ? "&" : "")}to={to:yyyy-MM-dd}";

            var url = $"{AppConfig.ChatUrl}Applicant/Get";

            if (!string.IsNullOrEmpty(query))
                url += "?" + query;

            return await _http.GetFromJsonAsync<ApplicantDashboardDto>(url)
                   ?? new ApplicantDashboardDto();
        }
        // Get all applicants
        public async Task<List<ApplicantListDto>> GetApplicantsAsync()
        {
            var response = await _http.GetAsync($"{AppConfig.ChatUrl}Applicant/GetAll");
            response.EnsureSuccessStatusCode(); // throws if not 2xx

            var applicants = await response.Content.ReadFromJsonAsync<List<ApplicantListDto>>();
            return applicants ?? new List<ApplicantListDto>();
        }

        // Create new applicant
        public async Task<(bool Success, string Error)> CreateApplicantAsync(
       CreateApplicantDto dto)
        {
            var url = $"{AppConfig.ChatUrl}Applicant/Create";
            var response = await _http.PostAsJsonAsync(url, dto);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        // Soft delete applicant
        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"{AppConfig.ChatUrl}Applicant/Delete/{id}");
            return response.IsSuccessStatusCode;
        }

        // Restore soft-deleted applicant
        public async Task<bool> RestoreAsync(int id)
        {
            var response = await _http.PutAsync($"{AppConfig.ChatUrl}Applicant/Restore/{id}/restore", null);
            return response.IsSuccessStatusCode;
        }

        // Update applicant status
        public async Task<UpdateApplicantStatusResponseDto?> UpdateApplicantStatusAsync(
           int id,
           UpdateApplicantStatusDto dto)
        {
            var response = await _http.PutAsJsonAsync(
                $"{AppConfig.ChatUrl}Applicant/UpdateStatus/{id}/status",
                dto);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<UpdateApplicantStatusResponseDto>();
        }
        public async Task<bool> UpdateApplicantInfoAsync(int id, UpdateApplicantInfoDto dto)
        {
            var response = await _http.PutAsJsonAsync(
                $"{AppConfig.ChatUrl}Applicant/Update/{id}",
                dto);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update failed: {content}");
            }

            return response.IsSuccessStatusCode;
        }
        public async Task<ApplicantDetailDto?> GetApplicantByIdAsync(int id)
        {
            var response = await _http.GetAsync($"{AppConfig.ChatUrl}Applicant/GetById/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ApplicantDetailDto>();
        }
    }
}

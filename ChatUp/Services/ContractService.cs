using ChatUp.Application.Features.Contracts.Commands;
using ChatUp.Application.Features.Contracts.DTOs;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace ChatUp.Services
{
    public class ContractService
    {
        private readonly HttpClient _http;

        public ContractService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ContractDto>> GetAllAsync()
        {
            var url = $"{AppConfig.ChatUrl}Contracts/GetAll";
            return await _http.GetFromJsonAsync<List<ContractDto>>(url) ?? new();
        }

        public async Task<ContractDto?> GetByIdAsync(int id)
        {
            var url = $"{AppConfig.ChatUrl}Contracts/GetById/{id}";
            return await _http.GetFromJsonAsync<ContractDto>(url);
        }

        public async Task<HttpResponseMessage> CreateAsync(CreateContractCommand contract)
        {
            var url = $"{AppConfig.ChatUrl}Contracts/Create";
            var response = await _http.PostAsJsonAsync(url, contract);
            return response;
        }

        public async Task UpdateAsync(int id, UpdateContractCommand contract)
        {
            var url = $"{AppConfig.ChatUrl}Contracts/Update/{id}";
            await _http.PutAsJsonAsync(url, contract);
        }

        public async Task DeleteAsync(int id)
        {
            var url = $"{AppConfig.ChatUrl}Contracts/Delete/{id}";
            await _http.DeleteAsync(url);

        }
    }
}

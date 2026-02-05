using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatUp.Services
{
    public class TicketService
    {
        private readonly HttpClient _http;
        public TicketService(HttpClient http) => _http = http;

        public async Task<List<TicketDto>> GetTicketsAsync(int userId, int page = 1, int pageSize = 20)
        {
            var url = $"TicketMessage/GetTickets/GetTickets?userId={userId}&includeMessages=false&page={page}&pageSize={pageSize}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            return await _http.GetFromJsonAsync<List<TicketDto>>(url, options) ?? new List<TicketDto>();
        }

        public async Task<TicketDto?> GetTicketDetailsAsync(int ticketId)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            return await _http.GetFromJsonAsync<TicketDto>($"TicketMessage/GetTicket/{ticketId}", options);
        }

        public async Task<bool> UpdateTitleAsync(int ticketId, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle)) return false;
            var payload = new { TicketId = ticketId, NewTitle = newTitle };
            var res = await _http.PutAsJsonAsync("TicketMessage/UpdateTitle/update-title", payload);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> CloseTicketAsync(int ticketId, int updatedBy, string remarks)
        {
            var payload = new
            {
                TicketId = ticketId,
                NewStatus = TicketStatus.Closed.ToString(),
                UpdatedBy = updatedBy,
                UpdatedAt = DateTime.UtcNow,
                Remarks = remarks
            };
            var res = await _http.PutAsJsonAsync("Tickets/UpdateStatus", payload);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> CreateTicketAsync(CreateTicketCommand command)
        {
            var res = await _http.PostAsJsonAsync("Tickets/Create", command);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> ConvertToCaseAsync(int ticketId, int updatedBy)
        {
            var payload = new { updatedBy };
            var res = await _http.PutAsJsonAsync($"Tickets/UpdateIsCase/UpdateIsCase/{ticketId}?updatedBy={updatedBy}", payload);
            return res.IsSuccessStatusCode;
        }

        public async Task<int> GetTicketRatingAsync(int ticketId)
        {
            return await _http.GetFromJsonAsync<int>($"TicketMessage/GetTicketRating/GetTicketRating/{ticketId}");
        }

        public async Task<bool> SubmitRatingAsync(SubmitTicketRatingCommand command)
        {
            var res = await _http.PostAsJsonAsync("TicketMessage/SubmitRating/SubmitRating", command);
            return res.IsSuccessStatusCode;
        }
    }

}

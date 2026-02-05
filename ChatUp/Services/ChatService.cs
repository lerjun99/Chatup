using ChatUp.Application.Features.Messages.DTOs;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Application.Features.TicketMessage.DTOs;
using ChatUp.Application.Features.User.DTOs;
using ChatUp.Domain.Entities;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Fast.Components.FluentUI;
using System.Collections.Concurrent;
using static System.Net.WebRequestMethods;

namespace ChatUp.Services
{
    public class ChatService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly NavigationManager _navigation;
        public HubConnection? HubConnection => _hubConnection;
        public event Action<ChatMessage>? OnMessageReceived;
        public event Action<MessageDto>? OnMessageInboxReceived;
        public event Action<int, int, bool>? OnTypingReceived;
        public event Action<int>? OnOpenTicketCountChanged;
        private readonly HttpClient _http;
        public event Action<int, bool, DateTime>? OnUserStatusChanged;
        private static readonly ConcurrentDictionary<int, bool> UserStatuses = new();
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public ChatService(NavigationManager navigation, HttpClient http)
        {
            _navigation = navigation;
            _http = http;
        }

        public async Task StartAsync(int userId)
        {
            if (_hubConnection != null) return;

             _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{AppConfig.ChatUrl}chathub?userId={userId}")
            .WithAutomaticReconnect()
            .Build();

            _hubConnection.On<ChatMessage>("ReceiveMessage", message =>
            {
                OnMessageReceived?.Invoke(message);
            });

            _hubConnection.On<int, int, bool>("ReceiveTypingStatus", (senderId, receiverId, isTyping) =>
            {
                OnTypingReceived?.Invoke(senderId, receiverId, isTyping);
            });
            _hubConnection.On<int, bool, DateTime>("UserStatusChanged", (userId, isOnline, lastLogin) =>
            {
                OnUserStatusChanged?.Invoke(userId, isOnline, lastLogin);
            });
            _hubConnection.On<MessageDto>("ReceiveTicketMessage", message =>
            {
                OnMessageInboxReceived?.Invoke(message);
            });

            await _hubConnection.StartAsync();

            await EnsureConnected();
        }
        public async Task NotifyTicketUpdateAsync()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("BroadcastTicketUpdate");
            }
        }

        public async Task LoadOpenTicketCount(int userId)
        {
            try
            {
                string url = $"{AppConfig.ChatUrl}TicketMessage/GetTickets/GetTickets" +
                             $"?userId={userId}&includeMessages=false&page=1&pageSize=1";

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                var result = await _http.GetFromJsonAsync<TicketPagedResponse>(url, options);

                int openCount = result?.StatusCounts
                                ?.FirstOrDefault(s => s.Status == TicketStatus.Open)?.Count ?? 0;

                OnOpenTicketCountChanged?.Invoke(openCount);
            }
            catch
            {
                OnOpenTicketCountChanged?.Invoke(0);
            }
        }


        private async Task<bool> EnsureConnected()
        {
            if (_hubConnection == null)
                return false;

            // Already connected
            if (_hubConnection.State == HubConnectionState.Connected)
                return true;

            // If reconnecting, wait until fully restored
            if (_hubConnection.State == HubConnectionState.Reconnecting)
            {
                int retries = 0;
                while (_hubConnection.State == HubConnectionState.Reconnecting && retries < 15)
                {
                    await Task.Delay(200);
                    retries++;
                }
            }

            // If disconnected → reconnect
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return _hubConnection.State == HubConnectionState.Connected;
        }

        #region Conversation API

        public async Task<ChatConversationDto?> CreateConversation(string name, bool isGroup, List<int> userIds)
        {
            if (_hubConnection == null)
                throw new InvalidOperationException("Hub connection not initialized");

            if (_hubConnection.State != HubConnectionState.Connected)
                await _hubConnection.StartAsync(); // start if not connected

            var request = new
            {
                id = 0,
                name = name,
                isGroup = isGroup,
                userIds = userIds
            };
            var url = $"{AppConfig.ChatUrl}Messaging/CreateConversation/create";
            var response = await _http.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChatConversationDto>();
        }

        public async Task CloseConversation(int conversationId)
        {
            var response = await _http.PostAsJsonAsync($"Messaging/CloseConversation/{conversationId}", new { });
            response.EnsureSuccessStatusCode();
        }

        #endregion
        public async Task<List<UserDto>> GetUsersAsync(int? id = null)
        {
            try
            {
                string url = id.HasValue
            ? $"{AppConfig.ChatUrl}User/GetUserById/{id.Value}" // still returns a list
            : $"{AppConfig.ChatUrl}User/GetUsers";

                var users = await _http.GetFromJsonAsync<List<UserDto>>(url);
                return users ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load users: {ex.Message}");
                return new List<UserDto>(); // Ensure a value is always returned
            }
        }
       
        public async Task SendMessage( ChatMessage message)
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("SendMessage", message);
            }
        }
        public async Task JoinConversation(int conversationId)
        {
            if (_hubConnection == null)
                throw new InvalidOperationException("Hub connection not initialized");

            if (_hubConnection.State != HubConnectionState.Connected)
                await _hubConnection.StartAsync();

            await _hubConnection.SendAsync("JoinConversation", conversationId);
        }
        public async Task SendMessageInbox(MessageDto message)
        {

            if (_hubConnection == null)
                throw new InvalidOperationException("Hub connection not initialized");

            if (_hubConnection.State != HubConnectionState.Connected)
                await _hubConnection.StartAsync();

            await _hubConnection.SendAsync("SendTicketMessage", message);
        }
        public async Task SendTypingStatus(int senderId, int receiverId, bool isTyping)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("SendTypingStatus", senderId, receiverId, isTyping);
            }
        }
        public async Task<List<Client>> GetClientsAsync()
        {
            return await _http.GetFromJsonAsync<List<Client>>("Client/GetAll") ?? new List<Client>();
        }
        public async Task<List<ChatMessage>> GetMessagesAsync(int userId, int clientId)
        {
            return await _http.GetFromJsonAsync<List<ChatMessage>>(
                $"api/messages/{userId}/{clientId}") ?? new List<ChatMessage>();
        }
        public async Task SetUserOnlineAsync(int userId)
        {
            if (IsConnected)
            {
                await _hubConnection.InvokeAsync("UserOnline", userId);
            }
        }

        public async Task SetUserOfflineAsync(int userId)
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("UserOffline", userId);
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }
    }
}

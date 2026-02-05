using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Timers;

namespace ChatUp.Services
{
    public class SessionService : IDisposable
    {
        private readonly NavigationManager _nav;
        private readonly ISessionStorageService _session;
        private readonly ILocalStorageService _local;
        private readonly AuthenticationStateProvider _auth;
        private readonly ChatService _chatService;
        private readonly UserState _userState;
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _http;
        private readonly IDialogService _dialogService;

        private readonly System.Timers.Timer _idleTimer;
        private DateTime _lastActivity;
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(15); // 🕒 15 mins timeout
        private bool _isExpired = false;
        private bool _isChecking = false;

        public event Action? OnSessionExpired;

        public SessionService(
            NavigationManager nav,
            ISessionStorageService session,
            ILocalStorageService local,
            AuthenticationStateProvider auth,
            ChatService chatService,
            UserState userState,
            IJSRuntime jsRuntime,
            HttpClient http,
            IDialogService dialogService)
        {
            _nav = nav;
            _session = session;
            _local = local;
            _auth = auth;
            _chatService = chatService;
            _userState = userState;
            _jsRuntime = jsRuntime;
            _http = http;
            _dialogService = dialogService;

            _lastActivity = DateTime.Now;

            // 🔁 Timer checks every 30 seconds
            _idleTimer = new System.Timers.Timer(30000);
            _idleTimer.Elapsed += CheckIdleTime;
            _idleTimer.AutoReset = true;
            _idleTimer.Start();
        }

        /// <summary>
        /// Call this on any user activity (e.g., click, mouse move)
        /// </summary>
        public void UpdateActivity()
        {
            _lastActivity = DateTime.Now;
            if (_isExpired)
            {
                _isExpired = false;
                _idleTimer.Start();
            }
        }

        /// <summary>
        /// Periodically checks for inactivity
        /// </summary>
        private async void CheckIdleTime(object? sender, ElapsedEventArgs e)
        {
            if (_isChecking || _isExpired)
                return;

            _isChecking = true;

            try
            {
                if (DateTime.Now - _lastActivity > _idleTimeout)
                {
                    _isExpired = true;
                    _idleTimer.Stop();
                    await HandleAutoLogoutAsync();
                }
            }
            finally
            {
                _isChecking = false;
            }
        }

        /// <summary>
        /// Called when the session expires due to inactivity
        /// </summary>
        private async Task HandleAutoLogoutAsync()
        {
            try
            {
                Console.WriteLine("[SessionService] Session expired — auto logging out...");

                // 🔕 Notify chat that the user went offline
                var userId = _userState.UserId?.ToString();
                if (!string.IsNullOrEmpty(userId) && _chatService != null)
                {
                    await _chatService.SetUserOfflineAsync(_userState.UserId.Value);
                    Console.WriteLine($"User {userId} set offline via SignalR.");
                }

                // 🔐 Call logout API
                if (_userState.UserId != null)
                {
                    var logoutCommand = new { UserId = _userState.UserId.Value };
                    var response = await _http.PostAsJsonAsync(AppConfig.ChatUrl + "Auth/Logout/logout", logoutCommand);

                    if (!response.IsSuccessStatusCode)
                        Console.WriteLine($"⚠ Logout API returned: {response.StatusCode}");
                }

                // 🧹 Clear both storages
                await _session.ClearAsync();
                await _local.ClearAsync();

                // 🔄 Clear UserState
                _userState.UserId = null;
                _userState.Username = null;
                _userState.UserType = 0;

                // 🔒 Notify auth state provider
                if (_auth is CustomAuthStateProvider customProvider)
                    customProvider.NotifyUserLogout();

                // 📴 Dispose chat connection
                if (_chatService != null)
                    await _chatService.DisposeAsync();

                // 🪟 JS log (optional)
                await _jsRuntime.InvokeVoidAsync("console.log", "Auto logout due to inactivity.");

                // 🚫 Trigger session-expired modal or page redirect
                OnSessionExpired?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SessionService] Auto logout failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Manual logout (called from UI button)
        /// </summary>
        public async Task LogoutAsync()
        {
            var dialog = await _dialogService.ShowConfirmationAsync(
                "Are you sure you want to log out?",
                "Yes", "No", "Warning"
            );

            var result = await dialog.Result;
            if (result.Cancelled) return;

            await HandleAutoLogoutAsync();
        }

        public void Dispose()
        {
            _idleTimer.Dispose();
        }
    }
}

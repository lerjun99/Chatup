using ChatUp.Application.Features.Notifications;

namespace ChatUp.Services
{
    /// <summary>
    /// The signed-in user's bell notifications for this circuit: the list, the unread count, and live arrivals from
    /// the chat hub. Shared by the bell (top bar) and the pages that clear notifications (the inbox).
    /// </summary>
    public sealed class NotificationCenter : IDisposable
    {
        private readonly HttpClient _http;
        private readonly ChatService _chat;
        private int? _userId;
        private bool _subscribed;

        public NotificationCenter(HttpClient http, ChatService chat)
        {
            _http = http;
            _chat = chat;
        }

        public List<NotificationDto> Items { get; private set; } = new();
        public int UnreadCount { get; private set; }
        public bool HasMore { get; private set; }
        public bool IsLoaded { get; private set; }

        /// <summary>The ticket open in the inbox: its reply notifications are read on arrival and make no noise.</summary>
        public int? ActiveTicketId { get; set; }

        /// <summary>Raised whenever the list or the count changes.</summary>
        public event Action? OnChange;

        /// <summary>Raised for a live notification that should alert the user (sound, toast, desktop popup).</summary>
        public event Action<NotificationDto>? OnAlert;

        public async Task InitializeAsync(int userId)
        {
            if (!_subscribed)
            {
                _chat.OnNotificationReceived += HandleIncoming;
                _subscribed = true;
            }

            if (_userId == userId && IsLoaded)
                return;

            _userId = userId;
            await ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            if (_userId is not int userId) return;

            try
            {
                var list = await _http.GetFromJsonAsync<NotificationListDto>($"{AppConfig.ChatUrl}Notifications/Get/{userId}?take=20");
                if (list != null)
                {
                    Items = list.Items;
                    UnreadCount = list.UnreadCount;
                    HasMore = list.HasMore;
                    IsLoaded = true;
                    OnChange?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loading notifications failed: {ex.Message}");
            }
        }

        public async Task LoadMoreAsync()
        {
            if (_userId is not int userId || Items.Count == 0) return;

            try
            {
                var list = await _http.GetFromJsonAsync<NotificationListDto>(
                    $"{AppConfig.ChatUrl}Notifications/Get/{userId}?take=20&beforeId={Items[^1].Id}");
                if (list != null)
                {
                    Items.AddRange(list.Items.Where(n => Items.All(i => i.Id != n.Id)));
                    HasMore = list.HasMore;
                    UnreadCount = list.UnreadCount;
                    OnChange?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loading older notifications failed: {ex.Message}");
            }
        }

        public async Task MarkReadAsync(NotificationDto n)
        {
            if (_userId is not int userId || n.IsRead) return;

            n.IsRead = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);
            OnChange?.Invoke();

            try
            {
                var response = await _http.PostAsync($"{AppConfig.ChatUrl}Notifications/MarkRead/{n.Id}/{userId}", null);
                if (response.IsSuccessStatusCode)
                    UnreadCount = await response.Content.ReadFromJsonAsync<int>();
            }
            catch { /* the optimistic state stays; the next reload corrects it */ }

            OnChange?.Invoke();
        }

        public Task MarkAllReadAsync() => MarkReadWhereAsync(null);

        /// <summary>Clears the notifications of a ticket the user just opened.</summary>
        public Task MarkTicketReadAsync(int ticketId) =>
            Items.Any(n => n.TicketId == ticketId && !n.IsRead) || UnreadCount > 0
                ? MarkReadWhereAsync(ticketId)
                : Task.CompletedTask;

        private async Task MarkReadWhereAsync(int? ticketId)
        {
            if (_userId is not int userId) return;

            foreach (var n in Items.Where(n => !n.IsRead && (ticketId == null || n.TicketId == ticketId)))
                n.IsRead = true;
            if (ticketId == null) UnreadCount = 0;
            OnChange?.Invoke();

            try
            {
                var url = $"{AppConfig.ChatUrl}Notifications/MarkAllRead/{userId}" + (ticketId.HasValue ? $"?ticketId={ticketId}" : "");
                var response = await _http.PostAsync(url, null);
                if (response.IsSuccessStatusCode)
                    UnreadCount = await response.Content.ReadFromJsonAsync<int>();
            }
            catch { }

            OnChange?.Invoke();
        }

        private void HandleIncoming(NotificationDto n)
        {
            // Replies fold into one row server-side: replace the old copy and move it to the top
            Items.RemoveAll(i => i.Id == n.Id);
            Items.Insert(0, n);
            if (Items.Count > 100) Items.RemoveRange(100, Items.Count - 100);
            UnreadCount = n.UnreadCount > 0 ? n.UnreadCount : UnreadCount + 1;

            var isActiveChat = ActiveTicketId.HasValue && n.TicketId == ActiveTicketId && n.Type == NotificationTypes.TicketReply;
            if (isActiveChat)
            {
                // Already looking at it: read it straight away, quietly
                _ = MarkReadAsync(n);
            }
            else
            {
                OnAlert?.Invoke(n);
            }

            OnChange?.Invoke();
        }

        public void Dispose()
        {
            if (_subscribed)
                _chat.OnNotificationReceived -= HandleIncoming;
        }
    }
}

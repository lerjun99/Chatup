using Blazored.SessionStorage;
using ChatUp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

public class UserState
{
    private readonly IJSRuntime _js;
    private readonly ISessionStorageService _session;
    private readonly LinkService _linkService;

    // ✅ Capture Blazor SynchronizationContext (Dispatcher)
    private readonly SynchronizationContext? _syncContext;

    public UserState(IJSRuntime js, ISessionStorageService session, LinkService linkService)
    {
        _js = js;
        _session = session;
        _linkService = linkService;

        // Capture the current context (UI thread) for later use
        _syncContext = SynchronizationContext.Current;
    }

    private string? _username;
    private int? _usertype;
    private int? _userid;
    private string? _avatarUrl;
    private int? _clientId;
    public string? AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            if (_avatarUrl != value)
            {
                _avatarUrl = value;
                NotifyStateChanged();
            }
        }
    }

    public string? Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                NotifyStateChanged();
            }
        }
    }

    public int? UserType
    {
        get => _usertype;
        set
        {
            if (_usertype != value)
            {
                _usertype = value;
                NotifyStateChanged();
            }
        }
    }

    public int? UserId
    {
        get => _userid;
        set
        {
            if (_userid != value)
            {
                _userid = value;
                NotifyStateChanged();
            }
        }
    }
    public int? ClientId
    {
        get => _clientId;
        set
        {
            if (_clientId != value)
            {
                _clientId = value;
                NotifyStateChanged();
            }
        }
    }
    public event Action? OnChange;

    // ✅ Ensure UI-safe notification
    public void NotifyStateChanged()
    {
        if (OnChange == null)
            return;

        if (_syncContext != null)
        {
            // Marshal back to the Blazor UI thread
            _syncContext.Post(_ => OnChange?.Invoke(), null);
        }
        else
        {
            // Fallback (WASM sometimes runs without SynchronizationContext)
            OnChange?.Invoke();
        }
    }

    // ✅ Save username to localStorage
    public async Task SaveUsernameAsync()
    {
        if (!string.IsNullOrWhiteSpace(_username))
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "username", _username);
        }
    }

    // ✅ Load username from localStorage
    public async Task LoadUsernameAsync()
    {
        _username = await _js.InvokeAsync<string?>("localStorage.getItem", "username");
        NotifyStateChanged();
    }

    public async Task LoadAsync()
    {
        var encUserId = await _session.GetItemAsync<string>("UserId");
        var encUserType = await _session.GetItemAsync<string>("UserType");
        var encUsername = await _session.GetItemAsync<string>("Username");
        var encAvatar = await _session.GetItemAsync<string>("AvatarUrl");
        var encClientId = await _session.GetItemAsync<string>("ClientId");

        if (!string.IsNullOrEmpty(encUserId))
            UserId = int.Parse(_linkService.Unprotect(encUserId));

        if (!string.IsNullOrEmpty(encUserType))
            UserType = int.Parse(_linkService.Unprotect(encUserType));

        if (!string.IsNullOrEmpty(encUsername))
            Username = _linkService.Unprotect(encUsername);
        if (!string.IsNullOrEmpty(encAvatar))
            AvatarUrl = _linkService.Unprotect(encAvatar);
        if (!string.IsNullOrEmpty(encClientId))                               
            ClientId = int.Parse(_linkService.Unprotect(encClientId));
        NotifyStateChanged();
    }

    // ✅ Logout: clear username from memory and localStorage
    public async Task LogoutAsync()
    {
        _username = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", "username");
        NotifyStateChanged();
    }

    public async Task LoadFromSessionAsync(ISessionStorageService session)
    {
        UserId = await session.GetItemAsync<int?>("UserId");
        UserType = await session.GetItemAsync<int?>("UserType");
        Username = await session.GetItemAsync<string>("Username");
        AvatarUrl = await session.GetItemAsync<string>("AvatarUrl");
        ClientId = await session.GetItemAsync<int?>("ClientId");
    }

    public async Task SaveToSessionAsync(ISessionStorageService session)
    {
        if (UserId != null)
            await session.SetItemAsync("UserId", _linkService.Protect(UserId.ToString()!));

        if (UserType != null)
            await session.SetItemAsync("UserType", _linkService.Protect(UserType.ToString()!));

        if (!string.IsNullOrEmpty(Username))
            await session.SetItemAsync("Username", _linkService.Protect(Username!));

        if (!string.IsNullOrEmpty(AvatarUrl))
            await session.SetItemAsync("AvatarUrl", _linkService.Protect(AvatarUrl!));
        if (ClientId != null)                                              // 👈 NEW
            await session.SetItemAsync("ClientId", _linkService.Protect(ClientId.ToString()!));
    }

    public async Task ClearSessionAsync(ISessionStorageService session)
    {
        await session.RemoveItemAsync("UserId");
        await session.RemoveItemAsync("UserType");
        await session.RemoveItemAsync("Username");
        await session.RemoveItemAsync("AvatarUrl");
        await session.RemoveItemAsync("ClientId");
    }
}

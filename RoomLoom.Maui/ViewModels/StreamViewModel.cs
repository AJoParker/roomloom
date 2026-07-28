using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomLoom.Maui.Services;

namespace RoomLoom.Maui.ViewModels;

public partial class StreamViewModel : BaseViewModel
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IUserIdentity _identity;

    [ObservableProperty]
    private string _liveSessionId = string.Empty;

    [ObservableProperty]
    private string _statusText = "Loading...";

    public StreamViewModel(IHttpClientFactory httpFactory, IUserIdentity identity)
    {
        _httpFactory = httpFactory;
        _identity = identity;
    }

    public event EventHandler<TokenReadyEventArgs>? TokenReady;

    [RelayCommand]
    private async Task LoadAsync()
    {
        SetLoading();
        StatusText = "Fetching token...";
        try
        {
            var client = _httpFactory.CreateClient("RoomLoomApi");
            var url = $"/live-sessions/{Uri.EscapeDataString(LiveSessionId)}/token?participantId={Uri.EscapeDataString(_identity.Current.Id)}";
            var payload = await client.GetFromJsonAsync<TokenPayload>(url);
            if (payload is null || string.IsNullOrEmpty(payload.Token) || string.IsNullOrEmpty(payload.Url))
            {
                SetError("Empty token response");
                StatusText = "Failed";
                return;
            }
            SetLoaded();
            StatusText = "Connecting...";
            TokenReady?.Invoke(this, new TokenReadyEventArgs(payload.Url, payload.Token));
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            StatusText = $"Failed: {ex.Message}";
        }
    }

    public void OnWebViewMessage(string raw)
    {
        try
        {
            var msg = System.Text.Json.JsonSerializer.Deserialize<WebViewMessage>(raw);
            if (msg is null) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusText = msg.Type switch
                {
                    "connected" => "Connected",
                    "disconnected" => "Disconnected",
                    "error" => $"Error: {msg.Message}",
                    _ => StatusText,
                };
            });
        }
        catch
        {
            // ignore malformed bridge payloads
        }
    }

    private sealed record TokenPayload(string Url, string Token);

    private sealed record WebViewMessage(string Type, string? Message);
}

public sealed class TokenReadyEventArgs : EventArgs
{
    public TokenReadyEventArgs(string url, string token)
    {
        Url = url;
        Token = token;
    }

    public string Url { get; }
    public string Token { get; }
}

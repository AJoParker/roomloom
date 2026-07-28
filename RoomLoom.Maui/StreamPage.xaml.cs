using System.Text.Json;
using RoomLoom.Maui.ViewModels;

namespace RoomLoom.Maui;

[QueryProperty(nameof(LiveSessionId), "liveSessionId")]
public partial class StreamPage : ContentPage
{
    private readonly StreamViewModel _vm;

    public string LiveSessionId
    {
        get => _vm.LiveSessionId;
        set => _vm.LiveSessionId = value;
    }

    public StreamPage(StreamViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.TokenReady += OnTokenReady;
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    protected override async void OnDisappearing()
    {
        _vm.TokenReady -= OnTokenReady;
        try
        {
            await WebView.EvaluateJavaScriptAsync("window.stopStream && window.stopStream()");
        }
        catch
        {
            // best-effort teardown
        }
        base.OnDisappearing();
    }

    private async void OnTokenReady(object? sender, TokenReadyEventArgs e)
    {
        var configJson = JsonSerializer.Serialize(new { url = e.Url, token = e.Token });
        try
        {
            await WebView.EvaluateJavaScriptAsync($"window.startStream({configJson})");
        }
        catch (Exception ex)
        {
            _vm.OnWebViewMessage($"{{\"type\":\"error\",\"message\":\"InvokeJs failed: {ex.Message}\"}}");
        }
    }

    private void OnRawMessageReceived(object? sender, HybridWebViewRawMessageReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Message))
            _vm.OnWebViewMessage(e.Message);
    }
}

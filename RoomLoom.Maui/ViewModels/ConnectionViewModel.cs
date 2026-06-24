using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomLoom.Maui.Models;
using RoomLoom.Maui.Services;

namespace RoomLoom.Maui.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly ISessionConnection _connection;
    private readonly IUserIdentity _identity;
    private bool _subscribed;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private string _sessionId = "phase1-session";

    [ObservableProperty]
    private string _eventsText = "(nothing yet)";

    public ConnectionViewModel(ISessionConnection connection, IUserIdentity identity)
    {
        _connection = connection;
        _identity = identity;
    }

    public void Subscribe()
    {
        if (_subscribed) return;
        StatusText = _connection.State.ToString();
        _connection.StateChanged += OnStateChanged;
        _connection.ParticipantJoined += OnParticipantJoined;
        _connection.ParticipantLeft += OnParticipantLeft;
        _subscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_subscribed) return;
        _connection.StateChanged -= OnStateChanged;
        _connection.ParticipantJoined -= OnParticipantJoined;
        _connection.ParticipantLeft -= OnParticipantLeft;
        _subscribed = false;
    }

    [RelayCommand]
    private async Task ConnectAndJoinAsync()
    {
        try
        {
            await _connection.ConnectAsync();
            await _connection.JoinSessionAsync(SessionId, _identity.Current);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
        }
    }

    private void OnStateChanged(object? sender, ConnectionState state) =>
        MainThread.BeginInvokeOnMainThread(() => StatusText = state.ToString());

    private void OnParticipantJoined(object? sender, Participant p) =>
        MainThread.BeginInvokeOnMainThread(() => EventsText = $"Joined: {p.Name} ({p.Id})");

    private void OnParticipantLeft(object? sender, Participant p) =>
        MainThread.BeginInvokeOnMainThread(() => EventsText = $"Left: {p.Name} ({p.Id})");
}

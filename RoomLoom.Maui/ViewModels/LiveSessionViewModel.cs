using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomLoom.Maui.Models;
using RoomLoom.Maui.Services;

namespace RoomLoom.Maui.ViewModels;

public partial class LiveSessionViewModel : BaseViewModel, IDisposable
{
    private readonly ISessionConnection _connection;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IUserIdentity _identity;
    private bool _subscribed;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private string _lifecycleText = "Scheduled";

    [ObservableProperty]
    private LifecycleBadge _lifecycle = LifecycleBadge.Scheduled;

    [ObservableProperty]
    private ObservableCollection<Participant> _participants = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinVideoCommand))]
    private LiveSession? _liveSession;

    public LiveSessionViewModel(ISessionConnection connection, IHttpClientFactory httpFactory, IUserIdentity identity)
    {
        _connection = connection;
        _httpFactory = httpFactory;
        _identity = identity;
    }

    [RelayCommand]
    private async Task JoinAsync()
    {
        SetLoading();
        SubscribeIfNeeded();
        ConnectionState = _connection.State;

        try
        {
            await _connection.ConnectAsync();
            await _connection.JoinSessionAsync(SessionId, _identity.Current);
            var initial = await _connection.GetParticipantsAsync(SessionId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Participants = new ObservableCollection<Participant>(initial);
                if (Participants.Count == 0)
                    SetEmpty();
                else
                    SetLoaded();
            });
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        Unsubscribe();
        try
        {
            await _connection.LeaveSessionAsync(SessionId);
        }
        catch
        {
            // best-effort
        }
    }

    [RelayCommand]
    private async Task GoLiveAsync()
    {
        try
        {
            var client = _httpFactory.CreateClient("RoomLoomApi");
            var response = await client.PostAsync($"/sessions/{Uri.EscapeDataString(SessionId)}/go-live", content: null);
            if (!response.IsSuccessStatusCode)
            {
                LifecycleText = $"Go-live failed: {(int)response.StatusCode}";
                Lifecycle = LifecycleBadge.Error;
            }
        }
        catch (Exception ex)
        {
            LifecycleText = $"Go-live error: {ex.Message}";
            Lifecycle = LifecycleBadge.Error;
        }
    }

    private void SubscribeIfNeeded()
    {
        if (_subscribed) return;
        _connection.StateChanged += OnStateChanged;
        _connection.ParticipantJoined += OnParticipantJoined;
        _connection.ParticipantLeft += OnParticipantLeft;
        _connection.SessionLive += OnSessionLive;
        _connection.SessionEnded += OnSessionEnded;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _connection.StateChanged -= OnStateChanged;
        _connection.ParticipantJoined -= OnParticipantJoined;
        _connection.ParticipantLeft -= OnParticipantLeft;
        _connection.SessionLive -= OnSessionLive;
        _connection.SessionEnded -= OnSessionEnded;
        _subscribed = false;
    }

    private void OnStateChanged(object? sender, ConnectionState state) =>
        MainThread.BeginInvokeOnMainThread(() => ConnectionState = state);

    private void OnParticipantJoined(object? sender, Participant p) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!Participants.Any(x => x.Id == p.Id))
                Participants.Add(p);
            if (Participants.Count > 0 && State == ViewState.Empty)
                SetLoaded();
        });

    private void OnParticipantLeft(object? sender, Participant p) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = Participants.FirstOrDefault(x => x.Id == p.Id);
            if (existing is not null)
                Participants.Remove(existing);
            if (Participants.Count == 0 && State == ViewState.Loaded)
                SetEmpty();
        });

    private void OnSessionLive(object? sender, LiveSession live) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LiveSession = live;
            LifecycleText = $"Live (room {live.MediaRoomId})";
            Lifecycle = LifecycleBadge.Live;
        });

    private void OnSessionEnded(object? sender, LiveSession live) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LiveSession = null;
            LifecycleText = "Ended";
            Lifecycle = LifecycleBadge.Ended;
        });

    [RelayCommand(CanExecute = nameof(CanJoinVideo))]
    private async Task JoinVideoAsync()
    {
        if (LiveSession is null) return;
        await Shell.Current.GoToAsync($"StreamPage?liveSessionId={Uri.EscapeDataString(LiveSession.Id)}");
    }

    private bool CanJoinVideo() => LiveSession is not null;

    public void Dispose() => Unsubscribe();
}

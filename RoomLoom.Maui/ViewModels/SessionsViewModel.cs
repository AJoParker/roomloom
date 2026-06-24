using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomLoom.Maui.Models;
using RoomLoom.Maui.Services;

namespace RoomLoom.Maui.ViewModels;

public partial class SessionsViewModel : BaseViewModel
{
    private readonly ISessionsApi _sessionsApi;

    [ObservableProperty]
    private ObservableCollection<ScheduledSession> _sessions = new();

    public SessionsViewModel(ISessionsApi sessionsApi)
    {
        _sessionsApi = sessionsApi;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        SetLoading();
        try
        {
            var fresh = await _sessionsApi.GetUpcomingAsync();
            Sessions = new ObservableCollection<ScheduledSession>(fresh);
            if (Sessions.Count == 0)
                SetEmpty();
            else
                SetLoaded();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenSessionAsync(ScheduledSession? session)
    {
        if (session is null) return;
        await Shell.Current.GoToAsync($"LivePage?sessionId={Uri.EscapeDataString(session.Id)}");
    }
}

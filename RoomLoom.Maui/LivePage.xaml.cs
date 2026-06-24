using RoomLoom.Maui.ViewModels;

namespace RoomLoom.Maui;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class LivePage : ContentPage
{
    private readonly LiveSessionViewModel _vm;

    public string SessionId
    {
        get => _vm.SessionId;
        set => _vm.SessionId = value;
    }

    public LivePage(LiveSessionViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.JoinCommand.ExecuteAsync(null);
    }

    protected override async void OnDisappearing()
    {
        await _vm.LeaveCommand.ExecuteAsync(null);
        _vm.Dispose();
        base.OnDisappearing();
    }
}

using RoomLoom.Maui.Models;
using RoomLoom.Maui.ViewModels;

namespace RoomLoom.Maui;

public partial class SessionsPage : ContentPage
{
    private readonly SessionsViewModel _vm;

    public SessionsPage(SessionsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ScheduledSession s)
        {
            SessionsList.SelectedItem = null;
            _vm.OpenSessionCommand.Execute(s);
        }
    }
}

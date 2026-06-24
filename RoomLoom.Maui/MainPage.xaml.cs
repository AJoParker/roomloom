using RoomLoom.Maui.ViewModels;

namespace RoomLoom.Maui;

public partial class MainPage : ContentPage
{
    private readonly ConnectionViewModel _vm;

    public MainPage(ConnectionViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Subscribe();
    }

    protected override void OnDisappearing()
    {
        _vm.Unsubscribe();
        base.OnDisappearing();
    }
}

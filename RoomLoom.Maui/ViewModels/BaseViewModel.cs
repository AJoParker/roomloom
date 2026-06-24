using CommunityToolkit.Mvvm.ComponentModel;

namespace RoomLoom.Maui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewState _state = ViewState.Loading;

    [ObservableProperty]
    private string? _errorMessage;

    protected void SetLoading()
    {
        ErrorMessage = null;
        State = ViewState.Loading;
    }

    protected void SetLoaded()
    {
        ErrorMessage = null;
        State = ViewState.Loaded;
    }

    protected void SetEmpty()
    {
        ErrorMessage = null;
        State = ViewState.Empty;
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
        State = ViewState.Error;
    }
}

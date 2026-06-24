namespace RoomLoom.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(LivePage), typeof(LivePage));
	}
}

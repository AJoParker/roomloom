using Microsoft.Extensions.Logging;
using RoomLoom.Maui.Services;
using RoomLoom.Maui.ViewModels;

namespace RoomLoom.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddHttpClient("RoomLoomApi", c => c.BaseAddress = new Uri(ApiEndpoints.ApiBaseUrl));
		builder.Services.AddSingleton<ISessionsApi, SessionsApi>();
		builder.Services.AddSingleton<ISessionConnection, SessionConnection>();
		builder.Services.AddSingleton<IUserIdentity, StubUserIdentity>();

		builder.Services.AddTransient<ConnectionViewModel>();
		builder.Services.AddTransient<SessionsViewModel>();
		builder.Services.AddTransient<LiveSessionViewModel>();

		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<SessionsPage>();
		builder.Services.AddTransient<LivePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

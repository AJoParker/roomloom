using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoomLoom.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    public RecordingMediaProvider Media { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMediaProvider>();
            services.AddSingleton<IMediaProvider>(Media);
        });
    }
}

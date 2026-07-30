using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoomLoom.Core.Interfaces;

namespace RoomLoom.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    public RecordingMediaProvider Media { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Tests always run the no-DB path (InMemory provider). Without this,
        // developer user-secrets leak a real connection string into the test
        // host and swap in the EF provider.
        builder.UseSetting("ConnectionStrings:RoomLoomDb", "");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMediaProvider>();
            services.AddSingleton<IMediaProvider>(Media);
        });
    }
}

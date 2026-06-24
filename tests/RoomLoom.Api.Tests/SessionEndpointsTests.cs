using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Tests;

public class SessionEndpointsTests
{
    [Fact]
    public async Task EndSession_ReturnsNotFound_ForUnknownLiveSession()
    {
        await using var factory = new TestWebAppFactory();
        using var http = factory.CreateClient();

        var response = await http.PostAsync("/live-sessions/does-not-exist/end", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSessions_ReturnsUpcomingFromProvider()
    {
        await using var factory = new TestWebAppFactory();
        using var http = factory.CreateClient();

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        var sessions = await http.GetFromJsonAsync<List<ScheduledSession>>("/sessions", jsonOptions);

        Assert.NotNull(sessions);
        Assert.Equal(2, sessions!.Count);
        Assert.Contains(sessions, s => s.Id == "session-1");
        Assert.Contains(sessions, s => s.Id == "session-2");
        Assert.All(sessions, s => Assert.Equal(SessionStatus.Scheduled, s.PlannedStatus));
    }

    [Fact]
    public async Task UnhandledException_Returns500_WithProblemDetailsBody()
    {
        await using var baseFactory = new TestWebAppFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISessionService>();
                services.AddScoped<ISessionService, ThrowingSessionService>();
            });
        });
        using var http = factory.CreateClient();

        var response = await http.PostAsync("/sessions/anything/go-live", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed class ThrowingSessionService : ISessionService
    {
        public Task<LiveSession> GoLiveAsync(string scheduledSessionId, CancellationToken ct = default)
            => throw new Exception("boom");

        public Task EndSessionAsync(string liveSessionId, CancellationToken ct = default)
            => throw new Exception("boom");
    }
}

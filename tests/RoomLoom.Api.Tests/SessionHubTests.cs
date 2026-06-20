using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace RoomLoom.Api.Tests;

public class SessionHubTests
{
    private static HubConnection BuildHubClient(TestWebAppFactory factory)
    {
        var server = factory.Server;
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, "hubs/session"),
                o =>
                {
                    o.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    o.Transports = HttpTransportType.LongPolling;
                })
            .AddJsonProtocol(o =>
                o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
    }

    [Fact]
    public async Task JoinSession_TwoClients_BothReceiveBroadcast()
    {
        await using var factory = new TestWebAppFactory();
        var sessionId = Guid.NewGuid().ToString();
        var alice = new Participant { Id = 1, Name = "Alice", Email = "alice@example.com" };
        var bob = new Participant { Id = 2, Name = "Bob", Email = "bob@example.com" };

        var aliceReceived = new TaskCompletionSource<string>();
        await using var aliceConn = BuildHubClient(factory);
        aliceConn.On<string>("TestMessage", msg => aliceReceived.TrySetResult(msg));
        await aliceConn.StartAsync();
        await aliceConn.InvokeAsync("JoinSession", sessionId, alice);

        await using var bobConn = BuildHubClient(factory);
        await bobConn.StartAsync();
        await bobConn.InvokeAsync("JoinSession", sessionId, bob);

        await bobConn.InvokeAsync("SendTestMessage", sessionId, "hello from bob");

        var winner = await Task.WhenAny(aliceReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(aliceReceived.Task, winner);
        Assert.Equal("hello from bob", await aliceReceived.Task);
    }

    [Fact]
    public async Task Disconnect_RemovesPresence_BroadcastsParticipantLeft()
    {
        await using var factory = new TestWebAppFactory();
        var sessionId = Guid.NewGuid().ToString();
        var alice = new Participant { Id = 1, Name = "Alice", Email = "alice@example.com" };
        var bob = new Participant { Id = 2, Name = "Bob", Email = "bob@example.com" };

        var leftSeen = new TaskCompletionSource<Participant>();
        await using var aliceConn = BuildHubClient(factory);
        aliceConn.On<Participant>("ParticipantLeft", p => leftSeen.TrySetResult(p));
        await aliceConn.StartAsync();
        await aliceConn.InvokeAsync("JoinSession", sessionId, alice);

        var bobConn = BuildHubClient(factory);
        await bobConn.StartAsync();
        await bobConn.InvokeAsync("JoinSession", sessionId, bob);

        var beforeCount = (await aliceConn.InvokeAsync<List<Participant>>("GetParticipants", sessionId)).Count;
        Assert.Equal(2, beforeCount);

        await bobConn.DisposeAsync();

        var winner = await Task.WhenAny(leftSeen.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(leftSeen.Task, winner);
        var leftParticipant = await leftSeen.Task;
        Assert.Equal(bob.Name, leftParticipant.Name);

        var afterCount = (await aliceConn.InvokeAsync<List<Participant>>("GetParticipants", sessionId)).Count;
        Assert.Equal(1, afterCount);
    }

    [Fact]
    public async Task GoLive_CreatesLiveSessionAndNotifies()
    {
        await using var factory = new TestWebAppFactory();
        var sessionId = Guid.NewGuid().ToString();
        var alice = new Participant { Id = 1, Name = "Alice", Email = "alice@example.com" };

        var liveSeen = new TaskCompletionSource<LiveSession>();
        await using var aliceConn = BuildHubClient(factory);
        aliceConn.On<LiveSession>("SessionLive", l => liveSeen.TrySetResult(l));
        await aliceConn.StartAsync();
        await aliceConn.InvokeAsync("JoinSession", sessionId, alice);

        using var http = factory.CreateClient();
        var goLiveResp = await http.PostAsync($"/sessions/{sessionId}/go-live", content: null);
        goLiveResp.EnsureSuccessStatusCode();

        var winner = await Task.WhenAny(liveSeen.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(liveSeen.Task, winner);
        var live = await liveSeen.Task;
        Assert.False(string.IsNullOrEmpty(live.Id));
        Assert.Equal(sessionId, live.ScheduledSessionId);
        Assert.StartsWith("recorded-room::", live.MediaRoomId);
        Assert.Single(factory.Media.CreatedRoomNames);

        var endResp = await http.PostAsync($"/live-sessions/{live.Id}/end", content: null);
        endResp.EnsureSuccessStatusCode();
        Assert.Single(factory.Media.EndedRoomIds);
        Assert.Equal(live.MediaRoomId, factory.Media.EndedRoomIds.First());
    }
}

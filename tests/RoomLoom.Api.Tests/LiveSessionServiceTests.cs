using RoomLoom.Api.Services;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Tests;

public class LiveSessionServiceTests
{
    [Fact]
    public void RegisterPresence_ReturnsFalse_OnDuplicate()
    {
        var service = new LiveSessionService();
        var sessionId = Guid.NewGuid().ToString();
        var connId = "conn-1";
        var participant = new Participant { Id = "p-1", Name = "P", Email = "p@example.com" };

        var firstResult = service.RegisterPresence(connId, sessionId, participant);
        var secondResult = service.RegisterPresence(connId, sessionId, participant);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Single(service.GetParticipants(sessionId));
    }

    [Fact]
    public async Task ConcurrentChurn_DoesNotDropLongLivedPresence()
    {
        var service = new LiveSessionService();
        var sessionId = Guid.NewGuid().ToString();

        const int longLivedCount = 50;
        var longLived = Enumerable.Range(0, longLivedCount)
            .Select(i => (ConnId: $"long-{i}", Participant: new Participant { Id = $"long-{i}", Name = $"long-{i}", Email = $"long-{i}@example.com" }))
            .ToArray();

        foreach (var (connId, participant) in longLived)
            service.RegisterPresence(connId, sessionId, participant);

        const int churnTasks = 32;
        const int churnPerTask = 200;

        var churn = Enumerable.Range(0, churnTasks).Select(taskIndex => Task.Run(() =>
        {
            for (var i = 0; i < churnPerTask; i++)
            {
                var connId = $"churn-{taskIndex}-{i}";
                var participant = new Participant { Id = connId, Name = connId, Email = $"{connId}@example.com" };
                service.RegisterPresence(connId, sessionId, participant);
                service.RemovePresence(connId);
            }
        })).ToArray();

        await Task.WhenAll(churn);

        var present = service.GetParticipants(sessionId);
        Assert.Equal(longLivedCount, present.Count);
        foreach (var (_, participant) in longLived)
            Assert.Contains(present, p => p.Name == participant.Name);
    }
}

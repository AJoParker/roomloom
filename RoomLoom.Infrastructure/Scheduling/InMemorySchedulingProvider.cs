using Microsoft.Extensions.Logging;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Infrastructure.Scheduling;

public class InMemorySchedulingProvider(ILogger<InMemorySchedulingProvider> logger) : ISchedulingProvider
{
    public Task<IReadOnlyList<ScheduledSession>> GetUpcomingSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetUpcomingSessionsAsync called for {UserId}", userId);
        return Task.FromResult<IReadOnlyList<ScheduledSession>>(new List<ScheduledSession>
        {
            new ScheduledSession
            {
                Id = "session-1",
                Title = "Test Session 1",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Participants = new List<Participant> { new Participant { Id = "user-1", Name = "User 1", Email="user1@example.com" }, new Participant { Id = "user-2", Name = "User 2", Email="user2@example.com" } }
            },
            new ScheduledSession
            {
                Id = "session-2",
                Title = "Test Session 2",
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(4),
                Participants = new List<Participant> { new Participant { Id = "user-3", Name = "User 3", Email="user3@example.com" }, new Participant { Id = "user-4", Name = "User 4", Email="user4@example.com" } }
            }
        });
    }

    public Task<ScheduledSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetSessionAsync called for {SessionId}", sessionId);
        return Task.FromResult<ScheduledSession?>(new ScheduledSession
        {
            Id = sessionId,
            Title = "Test Session",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Participants = new List<Participant> { new Participant { Id = "user-1", Name = "User 1", Email="user1@example.com" } }
        });
    }

    public Task<string> CreateSessionAsync(ScheduledSession session, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CreateSessionAsync called for {SessionId}", session.Id);
        return Task.FromResult("new-session-id");
    }

    public Task CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CancelSessionAsync called for {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public Task<bool> MarkSessionLiveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("MarkSessionLiveAsync called for {SessionId}", sessionId);
        return Task.FromResult(true);
    }

    public Task<bool> MarkSessionEndedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("MarkSessionEndedAsync called for {SessionId}", sessionId);
        return Task.FromResult(true);
    }
}

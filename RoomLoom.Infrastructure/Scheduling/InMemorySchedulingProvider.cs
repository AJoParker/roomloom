public class InMemorySchedulingProvider : ISchedulingProvider
{
    public Task<IReadOnlyList<ScheduledSession>> GetUpcomingSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("InMemorySchedulingProvider.GetUpcomingSessionsAsync called");
        Console.WriteLine($"UserId: {userId}");
        return Task.FromResult<IReadOnlyList<ScheduledSession>>(new List<ScheduledSession>
        {
            new ScheduledSession
            {
                Id = "session-1",
                Title = "Test Session 1",
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Participants = new List<Participant> { new Participant { Id = 1, Name = "User 1", Email="user1@example.com" }, new Participant { Id = 2, Name = "User 2", Email="user2@example.com" } }
            },
            new ScheduledSession
            {
                Id = "session-2",
                Title = "Test Session 2",
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(4),
                
                Participants = new List<Participant> { new Participant { Id = 3, Name = "User 3", Email="user3@example.com" }, new Participant { Id = 4, Name = "User 4", Email="user4@example.com" } }
            }
        });
    }

    public Task<ScheduledSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("InMemorySchedulingProvider.GetSessionAsync called");
        Console.WriteLine($"SessionId: {sessionId}");
        return Task.FromResult<ScheduledSession?>(new ScheduledSession
        {
            Id = sessionId,
            Title = "Test Session",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Participants = new List<Participant> { new Participant { Id = 1, Name = "User 1", Email="user1@example.com" } }
        });
    }

    public Task<string> CreateSessionAsync(ScheduledSession session, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("InMemorySchedulingProvider.CreateSessionAsync called");
        Console.WriteLine($"Session: {session}");
        return Task.FromResult("new-session-id");
    }

    public Task CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("InMemorySchedulingProvider.CancelSessionAsync called");
        Console.WriteLine($"SessionId: {sessionId}");
        return Task.CompletedTask;
    }

    public Task MarkSessionLiveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"InMemorySchedulingProvider.MarkSessionLiveAsync called for {sessionId}");
        return Task.CompletedTask;
    }

    public Task MarkSessionEndedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"InMemorySchedulingProvider.MarkSessionEndedAsync called for {sessionId}");
        return Task.CompletedTask;
    }
}
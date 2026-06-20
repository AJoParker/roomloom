public interface ISchedulingProvider
{
    //<summary>A snapshot of a scheduled session, including the media room details if the session is live.</summary>
    Task<IReadOnlyList<ScheduledSession>> GetUpcomingSessionsAsync(string userId, CancellationToken cancellationToken = default);

    //<summary>Get details of a scheduled session, including the media room details if the session is live.</summary>
    Task<ScheduledSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    //<summary>Creates a new scheduled session.</summary>
    Task<string> CreateSessionAsync(ScheduledSession session, CancellationToken cancellationToken = default);

    //<summary>Cancels a scheduled session.</summary>
    Task CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    //<summary>Marks a scheduled session as Live (called when the session goes live).</summary>
    Task MarkSessionLiveAsync(string sessionId, CancellationToken cancellationToken = default);

    //<summary>Marks a scheduled session as Ended (called when the live session terminates).</summary>
    Task MarkSessionEndedAsync(string sessionId, CancellationToken cancellationToken = default);
}
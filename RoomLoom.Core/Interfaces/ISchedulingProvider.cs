using RoomLoom.Core.Models;

namespace RoomLoom.Core.Interfaces;

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

    /// <summary>
    /// Marks a scheduled session as Live. Implementations MUST perform an optimistic conditional
    /// update (transition only from Scheduled). Returns true if the row was transitioned;
    /// false if it had already moved past Scheduled (concurrent go-live, cancel, or expiry).
    /// Callers that get false should treat the operation as a lost race.
    /// </summary>
    Task<bool> MarkSessionLiveAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a scheduled session as Ended. Implementations MUST perform an optimistic conditional
    /// update (transition only from Live). Returns true if the row was transitioned;
    /// false if it had already moved past Live.
    /// </summary>
    Task<bool> MarkSessionEndedAsync(string sessionId, CancellationToken cancellationToken = default);
}
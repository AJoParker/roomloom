using RoomLoom.Core.Exceptions;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Infrastructure.Sessions;

public class SessionService(
    ISchedulingProvider scheduling,
    IMediaProvider media,
    ILiveSessionService liveSessions,
    ISessionNotifier notifier) : ISessionService
{
    public async Task<LiveSession> GoLiveAsync(string scheduledSessionId, CancellationToken ct = default)
    {
        var scheduled = await scheduling.GetSessionAsync(scheduledSessionId, ct)
            ?? throw new SessionNotFoundException($"Scheduled session '{scheduledSessionId}' not found.");

        if (scheduled.PlannedStatus != SessionStatus.Scheduled)
            throw new InvalidSessionStateException(
                $"Scheduled session '{scheduledSessionId}' cannot go live from status '{scheduled.PlannedStatus}'.");

        var mediaRoomId = await media.CreateRoomAsync(scheduled.Title, ct);

        var transitioned = await scheduling.MarkSessionLiveAsync(scheduledSessionId, ct);
        if (!transitioned)
        {
            await media.EndRoomAsync(mediaRoomId, ct);
            throw new InvalidSessionStateException(
                $"Scheduled session '{scheduledSessionId}' was already transitioned out of Scheduled (race lost).");
        }

        var live = liveSessions.Create(scheduledSessionId, mediaRoomId);
        await notifier.NotifySessionLiveAsync(scheduledSessionId, live, ct);
        return live;
    }

    /// <summary>
    /// Ends a live session. Partial-failure modes:
    /// (1) EndRoomAsync throws: scheduled status stays Live, LiveSession stays in memory, retry is safe;
    /// (2) MarkSessionEndedAsync throws after the room is released: room is gone but DB still reads Live
    /// and the LiveSession remains in memory — manual cleanup or a follow-up sweep is required;
    /// (3) Notify throws after state is cleaned: clients miss the SessionEnded broadcast but eventually
    /// reconcile via reconnect/refresh, harmless.
    /// Accepted as a portfolio-scope limitation; revisit when a real failure mode manifests.
    /// </summary>
    public async Task EndSessionAsync(string liveSessionId, CancellationToken ct = default)
    {
        var live = liveSessions.Get(liveSessionId)
            ?? throw new SessionNotFoundException($"Live session '{liveSessionId}' not found.");

        await media.EndRoomAsync(live.MediaRoomId, ct);
        // Bool result intentionally ignored: a false here means the session was already marked Ended
        // (concurrent end-call won the race), which is the desired terminal state either way.
        _ = await scheduling.MarkSessionEndedAsync(live.ScheduledSessionId, ct);

        live.EndedTime = DateTimeOffset.UtcNow;
        live.RuntimeStatus = SessionStatus.Ended;

        liveSessions.Remove(liveSessionId);

        await notifier.NotifySessionEndedAsync(live.ScheduledSessionId, live, ct);
    }
}

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
            ?? throw new InvalidOperationException($"Scheduled session '{scheduledSessionId}' not found.");

        await scheduling.MarkSessionLiveAsync(scheduledSessionId, ct);

        var mediaRoomId = await media.CreateRoomAsync(scheduled.Title, ct);

        var live = liveSessions.Create(scheduledSessionId, mediaRoomId);

        await notifier.NotifySessionLiveAsync(scheduledSessionId, live, ct);

        return live;
    }

    public async Task EndSessionAsync(string liveSessionId, CancellationToken ct = default)
    {
        var live = liveSessions.Get(liveSessionId)
            ?? throw new InvalidOperationException($"Live session '{liveSessionId}' not found.");

        await media.EndRoomAsync(live.MediaRoomId, ct);
        await scheduling.MarkSessionEndedAsync(live.ScheduledSessionId, ct);
        liveSessions.Remove(liveSessionId);

        await notifier.NotifySessionEndedAsync(live.ScheduledSessionId, liveSessionId, ct);
    }
}

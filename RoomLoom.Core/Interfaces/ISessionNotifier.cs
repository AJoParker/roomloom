public interface ISessionNotifier
{
    Task NotifySessionLiveAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default);

    Task NotifySessionEndedAsync(string scheduledSessionId, string liveSessionId, CancellationToken ct = default);
}

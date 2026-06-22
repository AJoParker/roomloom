using RoomLoom.Core.Models;

namespace RoomLoom.Core.Interfaces;

public interface ISessionNotifier
{
    Task NotifySessionLiveAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default);

    Task NotifySessionEndedAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default);
}

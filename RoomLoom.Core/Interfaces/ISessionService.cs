using RoomLoom.Core.Models;

namespace RoomLoom.Core.Interfaces;

public interface ISessionService
{
    Task<LiveSession> GoLiveAsync(string scheduledSessionId, CancellationToken ct = default);

    Task EndSessionAsync(string liveSessionId, CancellationToken ct = default);
}

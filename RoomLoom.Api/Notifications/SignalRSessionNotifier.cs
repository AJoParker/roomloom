using Microsoft.AspNetCore.SignalR;
using RoomLoom.Api.Hubs;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Notifications;

public class SignalRSessionNotifier(IHubContext<SessionHub> hubContext) : ISessionNotifier
{
    public Task NotifySessionLiveAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default)
        => hubContext.Clients.Group(scheduledSessionId).SendAsync("SessionLive", liveSession, ct);

    public Task NotifySessionEndedAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default)
        => hubContext.Clients.Group(scheduledSessionId).SendAsync("SessionEnded", liveSession, ct);
}

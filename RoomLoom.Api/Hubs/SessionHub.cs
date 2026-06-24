using Microsoft.AspNetCore.SignalR;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Hubs;

public class SessionHub(ILiveSessionService liveSessions) : Hub
{
    private readonly ILiveSessionService _liveSessions = liveSessions;

    public async Task JoinSession(string sessionId, Participant participant)
    {
        var isNew = _liveSessions.RegisterPresence(Context.ConnectionId, sessionId, participant);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        if (isNew)
        {
            await Clients.Group(sessionId).SendAsync("ParticipantJoined", participant);
        }
    }

    public async Task LeaveSession(string sessionId)
    {
        var (removedSessionId, participant) = _liveSessions.RemovePresence(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        if (removedSessionId is not null && participant is not null)
        {
            await Clients.Group(removedSessionId).SendAsync("ParticipantLeft", participant);
        }
    }

    public IReadOnlyList<Participant> GetParticipants(string sessionId)
        => _liveSessions.GetParticipants(sessionId);

    public Task SendTestMessage(string sessionId, string message)
        => Clients.Group(sessionId).SendAsync("TestMessage", message);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (sessionId, participant) = _liveSessions.RemovePresence(Context.ConnectionId);
        if (sessionId is not null && participant is not null)
        {
            await Clients.Group(sessionId).SendAsync("ParticipantLeft", participant);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

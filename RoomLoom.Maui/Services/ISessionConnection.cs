using RoomLoom.Maui.Models;

namespace RoomLoom.Maui.Services;

public interface ISessionConnection
{
    ConnectionState State { get; }

    event EventHandler<ConnectionState>? StateChanged;
    event EventHandler<Participant>? ParticipantJoined;
    event EventHandler<Participant>? ParticipantLeft;
    event EventHandler<LiveSession>? SessionLive;
    event EventHandler<LiveSession>? SessionEnded;

    Task ConnectAsync(CancellationToken ct = default);
    Task JoinSessionAsync(string sessionId, Participant me, CancellationToken ct = default);
    Task LeaveSessionAsync(string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<Participant>> GetParticipantsAsync(string sessionId, CancellationToken ct = default);
}

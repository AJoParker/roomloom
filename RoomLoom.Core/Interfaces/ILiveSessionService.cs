using RoomLoom.Core.Models;

namespace RoomLoom.Core.Interfaces;

public interface ILiveSessionService
{
    LiveSession? Get(string liveSessionId);

    LiveSession Create(string scheduledSessionId, string mediaRoomId);

    void Remove(string liveSessionId);

    /// <summary>
    /// Registers a connection's presence in a session. Returns true if this is a new registration;
    /// false if the same connection is already registered to the same session (idempotent join).
    /// </summary>
    bool RegisterPresence(string connectionId, string sessionId, Participant participant);

    (string? SessionId, Participant? Participant) RemovePresence(string connectionId);

    IReadOnlyList<Participant> GetParticipants(string sessionId);
}

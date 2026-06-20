public interface ILiveSessionService
{
    LiveSession? Get(string liveSessionId);

    LiveSession Create(string scheduledSessionId, string mediaRoomId);

    void Remove(string liveSessionId);

    void RegisterPresence(string connectionId, string sessionId, Participant participant);

    (string? SessionId, Participant? Participant) RemovePresence(string connectionId);

    IReadOnlyList<Participant> GetParticipants(string sessionId);
}

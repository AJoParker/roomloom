using System.Collections.Concurrent;

namespace RoomLoom.Api.Services;

public class LiveSessionService : ILiveSessionService
{
    private readonly ConcurrentDictionary<string, LiveSession> _liveSessions = new();
    private readonly ConcurrentDictionary<string, (string SessionId, Participant Participant)> _presenceByConnection = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Participant>> _participantsBySession = new();

    public LiveSession? Get(string liveSessionId)
        => _liveSessions.TryGetValue(liveSessionId, out var session) ? session : null;

    public LiveSession Create(string scheduledSessionId, string mediaRoomId)
    {
        var live = new LiveSession
        {
            Id = Guid.NewGuid().ToString(),
            ScheduledSessionId = scheduledSessionId,
            MediaRoomId = mediaRoomId,
            StartedTime = DateTimeOffset.UtcNow,
            RuntimeStatus = SessionStatus.Live,
        };
        _liveSessions[live.Id] = live;
        return live;
    }

    public void Remove(string liveSessionId)
        => _liveSessions.TryRemove(liveSessionId, out _);

    public void RegisterPresence(string connectionId, string sessionId, Participant participant)
    {
        _presenceByConnection[connectionId] = (sessionId, participant);
        var bucket = _participantsBySession.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, Participant>());
        bucket[connectionId] = participant;
    }

    public (string? SessionId, Participant? Participant) RemovePresence(string connectionId)
    {
        if (!_presenceByConnection.TryRemove(connectionId, out var entry))
            return (null, null);

        if (_participantsBySession.TryGetValue(entry.SessionId, out var bucket))
        {
            bucket.TryRemove(connectionId, out _);
            if (bucket.IsEmpty)
                _participantsBySession.TryRemove(entry.SessionId, out _);
        }

        return (entry.SessionId, entry.Participant);
    }

    public IReadOnlyList<Participant> GetParticipants(string sessionId)
        => _participantsBySession.TryGetValue(sessionId, out var bucket)
            ? bucket.Values.ToList()
            : Array.Empty<Participant>();
}

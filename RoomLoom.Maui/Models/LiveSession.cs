namespace RoomLoom.Maui.Models;

public sealed record LiveSession(
    string Id,
    string ScheduledSessionId,
    string MediaRoomId,
    DateTimeOffset StartedTime,
    DateTimeOffset? EndedTime,
    SessionStatus RuntimeStatus);

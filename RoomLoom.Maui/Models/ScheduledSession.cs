namespace RoomLoom.Maui.Models;

public sealed record ScheduledSession(
    string Id,
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    SessionStatus PlannedStatus);

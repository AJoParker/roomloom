namespace RoomLoom.Core.Models;

public class LiveSession
{
    public string Id { get; set; } = string.Empty;
    public string ScheduledSessionId { get; set; } = string.Empty;
    public DateTimeOffset StartedTime { get; set; }
    public DateTimeOffset? EndedTime { get; set; }
    public string MediaRoomId { get; set; } = string.Empty;
    public SessionStatus RuntimeStatus { get; set; } = SessionStatus.Scheduled;
}
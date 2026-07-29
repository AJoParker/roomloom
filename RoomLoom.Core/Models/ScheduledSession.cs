namespace RoomLoom.Core.Models;

public class ScheduledSession
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Participant? Host { get; set; }
    public List<Participant> Participants { get; set; } = new();
    public SessionStatus PlannedStatus { get; set; } = SessionStatus.Scheduled;
    public string? OwnerId { get; set; }
}
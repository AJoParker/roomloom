public record LiveSession
{
    public string Id { get; set; } = string.Empty;
    public string ScheduledSessionId { get; set; } = string.Empty;
    public DateTimeOffset StartedTime { get; set; }
    public DateTimeOffset? EndedTime { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
}
namespace RoomLoom.Api.BackgroundServices;

public class SessionExpiryOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
}

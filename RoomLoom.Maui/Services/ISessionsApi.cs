using RoomLoom.Maui.Models;

namespace RoomLoom.Maui.Services;

public interface ISessionsApi
{
    Task<IReadOnlyList<ScheduledSession>> GetUpcomingAsync(CancellationToken ct = default);
}

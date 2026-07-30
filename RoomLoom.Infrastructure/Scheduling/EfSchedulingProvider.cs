using Microsoft.EntityFrameworkCore;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;
using RoomLoom.Infrastructure.Persistence;

namespace RoomLoom.Infrastructure.Scheduling;

public class EfSchedulingProvider(RoomLoomDbContext db) : ISchedulingProvider
{
    public async Task<IReadOnlyList<ScheduledSession>> GetUpcomingSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Not owner-filtered: the join flow requires other users to see the
        // session. userId is unused until auth defines "my sessions" scoping.
        return await db.ScheduledSessions
            .AsNoTracking()
            .Where(s => s.PlannedStatus == SessionStatus.Scheduled
                     && s.EndTime >= DateTimeOffset.UtcNow)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public Task<ScheduledSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => db.ScheduledSessions
            .AsNoTracking()
            .Include(s => s.Host)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task<string> CreateSessionAsync(ScheduledSession session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(session.Id))
            session.Id = Guid.NewGuid().ToString();
        session.PlannedStatus = SessionStatus.Scheduled;

        db.ScheduledSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await db.ScheduledSessions
            .Where(s => s.Id == sessionId && s.PlannedStatus == SessionStatus.Scheduled)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.PlannedStatus, SessionStatus.Cancelled),
                cancellationToken);
    }

    public async Task<bool> MarkSessionLiveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var transitioned = await db.ScheduledSessions
            .Where(s => s.Id == sessionId && s.PlannedStatus == SessionStatus.Scheduled)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.PlannedStatus, SessionStatus.Live),
                cancellationToken);
        return transitioned == 1;
    }

    public async Task<bool> MarkSessionEndedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var transitioned = await db.ScheduledSessions
            .Where(s => s.Id == sessionId && s.PlannedStatus == SessionStatus.Live)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.PlannedStatus, SessionStatus.Ended),
                cancellationToken);
        return transitioned == 1;
    }
}

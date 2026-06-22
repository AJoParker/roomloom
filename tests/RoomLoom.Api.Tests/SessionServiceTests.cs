using RoomLoom.Api.Services;
using RoomLoom.Core.Exceptions;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;
using RoomLoom.Infrastructure.Sessions;

namespace RoomLoom.Api.Tests;

public class SessionServiceTests
{
    [Theory]
    [InlineData(SessionStatus.Live)]
    [InlineData(SessionStatus.Ended)]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Expired)]
    public async Task GoLive_ThrowsWhenPlannedStatusIsNotScheduled(SessionStatus startingStatus)
    {
        var scheduling = new StubSchedulingProvider
        {
            Session = new ScheduledSession
            {
                Id = "sched-1",
                Title = "Already in flight",
                PlannedStatus = startingStatus,
            },
        };
        var media = new RecordingMediaProvider();
        var liveSessions = new LiveSessionService();
        var notifier = new NoopNotifier();
        var service = new SessionService(scheduling, media, liveSessions, notifier);

        await Assert.ThrowsAsync<InvalidSessionStateException>(() => service.GoLiveAsync("sched-1"));

        Assert.Empty(media.CreatedRoomNames);
        Assert.False(scheduling.MarkLiveCalled);
        Assert.False(notifier.LiveCalled);
    }

    [Fact]
    public async Task GoLive_ThrowsNotFoundWhenScheduledSessionMissing()
    {
        var scheduling = new StubSchedulingProvider { Session = null };
        var media = new RecordingMediaProvider();
        var service = new SessionService(scheduling, media, new LiveSessionService(), new NoopNotifier());

        await Assert.ThrowsAsync<SessionNotFoundException>(() => service.GoLiveAsync("missing"));

        Assert.Empty(media.CreatedRoomNames);
        Assert.False(scheduling.MarkLiveCalled);
    }

    [Fact]
    public async Task GoLive_ThrowsConflict_AndReleasesRoom_WhenMarkLiveReportsRaceLost()
    {
        var scheduling = new StubSchedulingProvider
        {
            Session = new ScheduledSession
            {
                Id = "sched-1",
                Title = "race target",
                PlannedStatus = SessionStatus.Scheduled,
            },
            MarkLiveReturns = false,
        };
        var media = new RecordingMediaProvider();
        var service = new SessionService(scheduling, media, new LiveSessionService(), new NoopNotifier());

        await Assert.ThrowsAsync<InvalidSessionStateException>(() => service.GoLiveAsync("sched-1"));

        Assert.Single(media.CreatedRoomNames);
        Assert.Single(media.EndedRoomIds);
    }

    [Fact]
    public async Task GoLive_CreatesRoomBeforeMarkingLive()
    {
        var scheduling = new StubSchedulingProvider
        {
            Session = new ScheduledSession
            {
                Id = "sched-1",
                Title = "ready to go",
                PlannedStatus = SessionStatus.Scheduled,
            },
            FailMarkLive = true,
        };
        var media = new RecordingMediaProvider();
        var service = new SessionService(scheduling, media, new LiveSessionService(), new NoopNotifier());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GoLiveAsync("sched-1"));

        Assert.Single(media.CreatedRoomNames);
        Assert.True(scheduling.MarkLiveCalled);
    }

    private sealed class StubSchedulingProvider : ISchedulingProvider
    {
        public ScheduledSession? Session { get; set; }
        public bool FailMarkLive { get; set; }
        public bool MarkLiveCalled { get; private set; }

        public Task<ScheduledSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Session);

        public bool MarkLiveReturns { get; set; } = true;

        public Task<bool> MarkSessionLiveAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            MarkLiveCalled = true;
            if (FailMarkLive)
                throw new InvalidOperationException("simulated mark-live failure");
            return Task.FromResult(MarkLiveReturns);
        }

        public Task<IReadOnlyList<ScheduledSession>> GetUpcomingSessionsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ScheduledSession>>(Array.Empty<ScheduledSession>());

        public Task<string> CreateSessionAsync(ScheduledSession session, CancellationToken cancellationToken = default)
            => Task.FromResult(session.Id);

        public Task CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> MarkSessionEndedAsync(string sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class NoopNotifier : ISessionNotifier
    {
        public bool LiveCalled { get; private set; }

        public Task NotifySessionLiveAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default)
        {
            LiveCalled = true;
            return Task.CompletedTask;
        }

        public Task NotifySessionEndedAsync(string scheduledSessionId, LiveSession liveSession, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}

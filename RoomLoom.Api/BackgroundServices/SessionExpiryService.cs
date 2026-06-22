using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomLoom.Core.Models;
using RoomLoom.Infrastructure.Persistence;

namespace RoomLoom.Api.BackgroundServices;

public class SessionExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionExpiryService> _logger;
    private readonly TimeSpan _interval;

    public SessionExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionExpiryService> logger,
        IOptions<SessionExpiryOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = options.Value.Interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ExpireLapsedSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session expiry sweep failed");
            }
        }
    }

    private async Task ExpireLapsedSessionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomLoomDbContext>();

        var now = DateTimeOffset.UtcNow;

        var expiredCount = await db.ScheduledSessions
            .Where(s => s.PlannedStatus == SessionStatus.Scheduled
                    && s.EndTime < now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    s => s.PlannedStatus,
                    SessionStatus.Expired),
                ct);

        if (expiredCount > 0)
            _logger.LogInformation("Expired {Count} lapsed sessions", expiredCount);
    }
}
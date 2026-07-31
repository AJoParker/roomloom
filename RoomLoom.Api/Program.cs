using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomLoom.Api.BackgroundServices;
using RoomLoom.Api.Contracts;
using RoomLoom.Api.Hubs;
using RoomLoom.Api.Notifications;
using RoomLoom.Api.Services;
using RoomLoom.Core.Exceptions;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;
using RoomLoom.Infrastructure.Media;
using RoomLoom.Infrastructure.Persistence;
using RoomLoom.Infrastructure.Scheduling;
using RoomLoom.Infrastructure.Sessions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services
    .AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment())
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("RoomLoomDb");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<RoomLoomDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<ISchedulingProvider, EfSchedulingProvider>();
    builder.Services.Configure<SessionExpiryOptions>(
        builder.Configuration.GetSection("SessionExpiry"));
    builder.Services.AddHostedService<SessionExpiryService>();
}
else
{
    builder.Services.AddScoped<ISchedulingProvider, InMemorySchedulingProvider>();
}

var liveKitSection = builder.Configuration.GetSection("LiveKit");
builder.Services.Configure<LiveKitOptions>(liveKitSection);
if (!string.IsNullOrWhiteSpace(liveKitSection["Url"]))
{
    builder.Services.AddSingleton<IMediaProvider, LiveKitMediaProvider>();
}
else
{
    builder.Services.AddSingleton<IMediaProvider, FakeMediaProvider>();
}

builder.Services.AddSingleton<ILiveSessionService, LiveSessionService>();
builder.Services.AddSingleton<ISessionNotifier, SignalRSessionNotifier>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddProblemDetails();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/sessions", async (
    ISchedulingProvider scheduling,
    CancellationToken ct,
    string? userId = null) =>
{
    var sessions = await scheduling.GetUpcomingSessionsAsync(userId ?? "dev-user", ct);
    return Results.Ok(sessions);
});

app.MapPost("/sessions", async (
    CreateSessionRequest request,
    ISchedulingProvider scheduling,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Title is required." });

    var session = new ScheduledSession
    {
        Title = request.Title.Trim(),
        StartTime = request.StartTime,
        // No end-time field in the create flow; one hour is the default.
        EndTime = request.StartTime.AddHours(1),
    };

    var id = await scheduling.CreateSessionAsync(session, ct);
    return Results.Created($"/room/{id}", new { id });
});

app.MapPost("/sessions/{id}/go-live", async (string id, ISessionService sessions, CancellationToken ct) =>
{
    try
    {
        var live = await sessions.GoLiveAsync(id, ct);
        return Results.Ok(live);
    }
    catch (SessionNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidSessionStateException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapGet("/live-sessions/{id}/token", async (
    string id,
    IMediaProvider media,
    ILiveSessionService liveSessions,
    IOptions<LiveKitOptions> liveKitOpts,
    CancellationToken ct,
    string? participantId = null) =>
{
    var live = liveSessions.Get(id);
    if (live is null)
        return Results.NotFound(new { error = $"Live session '{id}' not found." });

    var token = await media.GenerateJoinTokenAsync(live.MediaRoomId, participantId ?? "anonymous", ct);
    return Results.Ok(new { url = liveKitOpts.Value.Url, token });
});

app.MapPost("/live-sessions/{id}/end", async (string id, ISessionService sessions, CancellationToken ct) =>
{
    try
    {
        await sessions.EndSessionAsync(id, ct);
        return Results.NoContent();
    }
    catch (SessionNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapHub<SessionHub>("/hubs/session");

app.MapRazorPages();

app.UseHttpsRedirection();

app.Run();

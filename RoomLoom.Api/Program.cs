using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoomLoom.Api.BackgroundServices;
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
    builder.Services.Configure<SessionExpiryOptions>(
        builder.Configuration.GetSection("SessionExpiry"));
    builder.Services.AddHostedService<SessionExpiryService>();
}

builder.Services.AddScoped<ISchedulingProvider, InMemorySchedulingProvider>();
builder.Services.AddSingleton<IMediaProvider, FakeMediaProvider>();
builder.Services.AddSingleton<ILiveSessionService, LiveSessionService>();
builder.Services.AddSingleton<ISessionNotifier, SignalRSessionNotifier>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.UseHttpsRedirection();

app.Run();

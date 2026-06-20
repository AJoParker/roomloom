using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoomLoom.Api.BackgroundServices;
using RoomLoom.Api.Hubs;
using RoomLoom.Api.Notifications;
using RoomLoom.Api.Services;
using RoomLoom.Infrastructure.Media;
using RoomLoom.Infrastructure.Persistence;
using RoomLoom.Infrastructure.Sessions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services
    .AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment())
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<RoomLoomDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RoomLoomDb")));

builder.Services.AddScoped<ISchedulingProvider, InMemorySchedulingProvider>();
builder.Services.AddSingleton<IMediaProvider, FakeMediaProvider>();
builder.Services.AddSingleton<ILiveSessionService, LiveSessionService>();
builder.Services.AddSingleton<ISessionNotifier, SignalRSessionNotifier>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddHostedService<SessionExpiryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", (ISchedulingProvider scheduling) =>
{
    Console.WriteLine("Test endpoint called");
    return scheduling.GetUpcomingSessionsAsync("test-user");
});

app.MapPost("/sessions/{id}/go-live", async (string id, ISessionService sessions, CancellationToken ct) =>
{
    var live = await sessions.GoLiveAsync(id, ct);
    return Results.Ok(live);
});

app.MapPost("/live-sessions/{id}/end", async (string id, ISessionService sessions, CancellationToken ct) =>
{
    await sessions.EndSessionAsync(id, ct);
    return Results.NoContent();
});

app.MapHub<SessionHub>("/hubs/session");

app.UseHttpsRedirection();

app.Run();

public partial class Program;

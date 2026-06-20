using Microsoft.EntityFrameworkCore;
using RoomLoom.Api.BackgroundServices;
using RoomLoom.Api.Hubs;
using RoomLoom.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

builder.Services.AddDbContext<RoomLoomDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RoomLoomDb")));

builder.Services.AddScoped<ISchedulingProvider, InMemorySchedulingProvider>();

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

app.MapHub<SessionHub>("/hubs/session");

app.UseHttpsRedirection();

app.Run();

public partial class Program;

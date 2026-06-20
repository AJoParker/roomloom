using RoomLoom.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHostedService<SessionExpiryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", () =>
{
    Console.WriteLine("Test endpoint called");
    ISchedulingProvider schedulingProvider = new InMemorySchedulingProvider();
    return schedulingProvider.GetUpcomingSessionsAsync("test-user");
});

app.UseHttpsRedirection();

app.Run();

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Pages;

public class IndexModel(ISchedulingProvider scheduling) : PageModel
{
    public IReadOnlyList<ScheduledSession> Sessions { get; private set; } = [];

    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public DateTime StartTime { get; set; }

    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Sessions = await scheduling.GetUpcomingSessionsAsync("dev-user", ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            Error = "Title is required.";
            Sessions = await scheduling.GetUpcomingSessionsAsync("dev-user", ct);
            return Page();
        }

        var start = StartTime == default
            ? DateTimeOffset.UtcNow
            : new DateTimeOffset(DateTime.SpecifyKind(StartTime, DateTimeKind.Local));

        var session = new ScheduledSession
        {
            Title = Title.Trim(),
            StartTime = start,
            EndTime = start.AddHours(1),
        };

        var id = await scheduling.CreateSessionAsync(session, ct);
        return Redirect($"/room/{id}");
    }
}

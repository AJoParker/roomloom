using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Pages;

public class IndexModel(ISchedulingProvider scheduling) : PageModel
{
    public IReadOnlyList<ScheduledSession> Sessions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Sessions = await scheduling.GetUpcomingSessionsAsync("dev-user", ct);
    }
}

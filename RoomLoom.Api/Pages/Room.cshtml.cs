using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomLoom.Core.Interfaces;
using RoomLoom.Core.Models;

namespace RoomLoom.Api.Pages;

public class RoomModel(ISchedulingProvider scheduling) : PageModel
{
    public ScheduledSession Session { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken ct)
    {
        var session = await scheduling.GetSessionAsync(id, ct);
        if (session is null)
            return NotFound();

        Session = session;
        return Page();
    }
}

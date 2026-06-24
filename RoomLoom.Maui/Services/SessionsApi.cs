using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoomLoom.Maui.Models;

namespace RoomLoom.Maui.Services;

public sealed class SessionsApi : ISessionsApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IHttpClientFactory _httpFactory;

    public SessionsApi(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<IReadOnlyList<ScheduledSession>> GetUpcomingAsync(CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient("RoomLoomApi");
        var sessions = await client.GetFromJsonAsync<List<ScheduledSession>>("/sessions", JsonOptions, ct);
        return (IReadOnlyList<ScheduledSession>?)sessions ?? Array.Empty<ScheduledSession>();
    }
}

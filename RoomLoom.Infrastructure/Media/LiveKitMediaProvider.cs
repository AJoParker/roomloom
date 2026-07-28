using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;
using RoomLoom.Core.Interfaces;

namespace RoomLoom.Infrastructure.Media;

public sealed class LiveKitMediaProvider : IMediaProvider
{
    private readonly LiveKitOptions _options;
    private readonly RoomServiceClient _roomService;

    public LiveKitMediaProvider(IOptions<LiveKitOptions> options)
    {
        _options = options.Value;
        _roomService = new RoomServiceClient(ToHttpUrl(_options.Url), _options.ApiKey, _options.Secret);
    }

    public Task<string> CreateRoomAsync(string roomName, CancellationToken cancellationToken = default)
    {
        // LiveKit auto-creates rooms on first join; skip the RoomService round trip.
        return Task.FromResult(roomName);
    }

    public async Task<string> EndRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _roomService.DeleteRoom(new DeleteRoomRequest { Room = roomId });
        }
        catch
        {
            // Idempotent: already-deleted / unknown rooms are not a failure for the caller.
        }
        return roomId;
    }

    public Task<string> GenerateJoinTokenAsync(string roomId, string participantId, CancellationToken cancellationToken = default)
    {
        var token = new AccessToken(_options.ApiKey, _options.Secret)
            .WithIdentity(participantId)
            .WithGrants(new VideoGrants { RoomJoin = true, Room = roomId })
            .WithTtl(TimeSpan.FromHours(1));

        return Task.FromResult(token.ToJwt());
    }

    private static string ToHttpUrl(string url) => url switch
    {
        var u when u.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) => "http://" + u[5..],
        var u when u.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) => "https://" + u[6..],
        _ => url,
    };
}

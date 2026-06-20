namespace RoomLoom.Infrastructure.Media;

public class FakeMediaProvider : IMediaProvider
{
    public Task<string> CreateRoomAsync(string roomName, CancellationToken cancellationToken = default)
        => Task.FromResult($"fake-room::{roomName}::{Guid.NewGuid():N}");

    public Task<string> EndRoomAsync(string roomId, CancellationToken cancellationToken = default)
        => Task.FromResult(roomId);

    public Task<string> GenerateJoinTokenAsync(string roomId, string participantId, CancellationToken cancellationToken = default)
        => Task.FromResult($"fake-token::{roomId}::{participantId}");
}

using System.Collections.Concurrent;

namespace RoomLoom.Api.Tests;

public class RecordingMediaProvider : IMediaProvider
{
    public ConcurrentQueue<string> CreatedRoomNames { get; } = new();
    public ConcurrentQueue<string> EndedRoomIds { get; } = new();

    public Task<string> CreateRoomAsync(string roomName, CancellationToken cancellationToken = default)
    {
        CreatedRoomNames.Enqueue(roomName);
        return Task.FromResult($"recorded-room::{roomName}::{Guid.NewGuid():N}");
    }

    public Task<string> EndRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        EndedRoomIds.Enqueue(roomId);
        return Task.FromResult(roomId);
    }

    public Task<string> GenerateJoinTokenAsync(string roomId, string participantId, CancellationToken cancellationToken = default)
        => Task.FromResult($"recorded-token::{roomId}::{participantId}");
}

public interface IMediaProvider
{
    Task<string> CreateRoomAsync(string roomName, CancellationToken cancellationToken = default);

    Task<string> EndRoomAsync(string roomId, CancellationToken cancellationToken = default);

    Task<string> GenerateJoinTokenAsync(string roomId, string participantId, CancellationToken cancellationToken = default);
}
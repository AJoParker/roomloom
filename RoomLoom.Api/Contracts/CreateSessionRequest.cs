namespace RoomLoom.Api.Contracts;

public record CreateSessionRequest(string Title, DateTimeOffset StartTime);

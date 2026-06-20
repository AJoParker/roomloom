using Microsoft.AspNetCore.SignalR;

namespace RoomLoom.Api.Hubs;

public class SessionHub : Hub
{
    public Task JoinSession(string sessionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    public Task SendTestMessage(string sessionId, string message)
        => Clients.Group(sessionId).SendAsync("TestMessage", message);
}

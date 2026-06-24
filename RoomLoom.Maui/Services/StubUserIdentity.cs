using RoomLoom.Maui.Models;

namespace RoomLoom.Maui.Services;

public sealed class StubUserIdentity : IUserIdentity
{
    public Participant Current { get; } = new("maui-user", "MAUI", "maui@local");
}

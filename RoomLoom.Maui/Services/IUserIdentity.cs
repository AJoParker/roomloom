using RoomLoom.Maui.Models;

namespace RoomLoom.Maui.Services;

public interface IUserIdentity
{
    Participant Current { get; }
}

namespace RoomLoom.Core.Exceptions;

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(string message) : base(message) { }
}

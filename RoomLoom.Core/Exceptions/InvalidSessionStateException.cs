namespace RoomLoom.Core.Exceptions;

public class InvalidSessionStateException : Exception
{
    public InvalidSessionStateException(string message) : base(message) { }
}

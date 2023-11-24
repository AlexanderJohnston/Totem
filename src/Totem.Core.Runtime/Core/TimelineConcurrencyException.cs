namespace Totem;

public sealed class TimelineConcurrencyException : Exception
{
    public TimelineConcurrencyException(string? message, Exception? inner = null) : base(message, inner)
    { }
}

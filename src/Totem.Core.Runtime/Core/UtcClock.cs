namespace Totem.Core;

public sealed class UtcClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

namespace Realizer.Runtime.Signals.Events;
public class SignalThreaded : IEvent
{
    public SignalThreaded(Id memoryId, DiscordMessage message)
    {
        MemoryId = memoryId;
        Message = message;
    }

    public Id MemoryId { get; }

    public DiscordMessage Message { get; }


}

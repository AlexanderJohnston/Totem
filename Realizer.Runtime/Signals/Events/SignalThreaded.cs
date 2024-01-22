namespace Realizer.Runtime.Signals.Events;
public class SignalThreaded : IEvent
{
    public SignalThreaded(Id memoryId, DiscordMessage message)
    {
        ThreadId = memoryId;
        Message = message;
    }

    public Id ThreadId { get; }

    public DiscordMessage Message { get; }


}

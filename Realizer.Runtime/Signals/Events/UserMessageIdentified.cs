using Memory.Converse;

namespace Realizer.Runtime.Signals.Events;
public class UserMessageIdentified : IEvent
{
    // Take in a memory id and a signal
    public UserMessageIdentified(Id memoryId, DiscordMessage signal)
    {
        ThreadId = memoryId;
        Signal = signal;
    }
    public Id ThreadId { get; }
    public DiscordMessage Signal { get; }
}

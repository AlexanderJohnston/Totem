using Memory.Converse;

namespace Realizer.Runtime.Signals.Events;
public class UserMessageIdentified : IEvent
{
    // Take in a memory id and a signal
    public UserMessageIdentified(Id memoryId, DiscordMessage signal)
    {
        MemoryId = memoryId;
        Signal = signal;
    }
    public Id MemoryId { get; }
    public DiscordMessage Signal { get; }
}

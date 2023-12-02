namespace Realizer.Runtime.Signals.Events;

public class CommandIdentified : IEvent
{
    // Take an Id and string containing the result text
    public CommandIdentified(Id memoryId, string result)
    {
        MemoryId = memoryId;
        Result = result;
    }
    public Id MemoryId { get; }
    public string Result { get; }
}

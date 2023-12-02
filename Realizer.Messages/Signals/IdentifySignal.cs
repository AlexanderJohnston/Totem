using Memory.Converse;
using Realizer.Runtime.Signals;

namespace Realizer.Messages.Signals;
public class IdentifySignal : IWorkflowCommand
{
    public IdentifySignal(Id memoryId, DiscordMessage message)
    {
        MemoryId = memoryId;
        Signal = message;
    }

    public Id MemoryId { get; }
    public DiscordMessage Signal { get; }
}

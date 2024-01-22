using Memory.Converse;
using Realizer.Runtime.Signals;

namespace Realizer.Messages.Signals;
public class IdentifySignal : IWorkflowCommand
{
    public IdentifySignal(Id memoryId, DiscordMessage message)
    {
        ThreadId = memoryId;
        Signal = message;
    }

    public Id ThreadId { get; }
    public DiscordMessage Signal { get; }
}

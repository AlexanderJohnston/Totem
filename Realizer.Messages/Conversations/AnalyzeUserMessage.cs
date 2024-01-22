using Realizer.Runtime.Signals;

namespace Realizer.Messages.Conversations;
public class AnalyzeUserMessage : IWorkflowCommand
{
    // Take in a memory id and a signal
    public AnalyzeUserMessage(Id memoryId, DiscordMessage signal)
    {
        ThreadId = memoryId;
        Signal = signal;
    }
    public Id ThreadId { get; }
    public DiscordMessage Signal { get; }
}

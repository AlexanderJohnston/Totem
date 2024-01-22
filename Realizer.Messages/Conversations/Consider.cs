using Realizer.Runtime.Signals;

namespace Realizer.Messages.Conversations;

public class Consider : IWorkflowCommand
{
    //new Consider(e.ThreadId, e.Message, e.State));
    public Consider(Id threadId, DiscordMessage message, ConversationState state)
    {
        ThreadId = threadId;
        Message = message;
        State = state;
    }
    public Id ThreadId { get; }
    public DiscordMessage Message { get; }
    public ConversationState State { get; }
}

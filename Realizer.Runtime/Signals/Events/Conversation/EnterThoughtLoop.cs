using Realizer.Messages.Conversations;

namespace Realizer.Runtime.Signals.Events.Conversation;
public class EnterThoughtLoop : IEvent
{
    public EnterThoughtLoop(Id memoryId, DiscordMessage message, ConversationState state)
    {
        ThreadId = memoryId;
        Message = message;
        State = state;
    }

    public Id ThreadId { get; }
    public DiscordMessage Message { get; }
    public ConversationState State { get; }
}

namespace Realizer.Runtime.Signals.Events.Conversation;
public class ConversationEnded : IEvent
{
    public ConversationEnded(Id threadId, Id userId)
    {
        ThreadId = threadId;
        UserId = userId;
    }
    public Id ThreadId { get; }
    public Id UserId { get; }
}

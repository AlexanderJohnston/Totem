namespace Realizer.Runtime.Signals.Events.Conversation;
public class ConversationStarted : IEvent
{
    public ConversationStarted(Id threadId, Id userId, string message)
    {
        ThreadId = threadId;
        UserId = userId;
        Message = message;
    }
    public Id ThreadId { get; }
    public Id UserId { get; }
    public string Message { get; }
}

namespace Realizer.Messages.Conversations;
public class EndConversation : IWorkflowCommand
{
    public EndConversation(Id threadId, Id userId)
    {
        ThreadId = threadId;
        UserId = userId;
    }

    public Id ThreadId { get; }
    public Id UserId { get; }
}

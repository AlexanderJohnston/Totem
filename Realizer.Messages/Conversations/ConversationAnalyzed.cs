namespace Realizer.Messages.Conversations;
public class ConversationAnalyzed : IWorkflowCommand
{
    public ConversationAnalyzed(Id threadId, ConversationState newState)
    {
        ThreadId = threadId;
        NewState = newState;
    }

    public Id ThreadId { get; }
    public ConversationState NewState { get; }
}

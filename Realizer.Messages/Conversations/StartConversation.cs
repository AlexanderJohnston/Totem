namespace Realizer.Messages.Conversations;

public class StartConversation : IWorkflowCommand
{
    public StartConversation(Id threadId, Id userId, string initialMessage)
    {
        ThreadId = threadId;
        UserId = userId;
        InitialMessage = initialMessage;
    }

    public Id ThreadId { get; }
    public Id UserId { get; }
    public string InitialMessage { get; }
}

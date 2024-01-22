namespace Realizer.Messages.Conversations;
public class ChangeTopic : IWorkflowCommand
{
    public ChangeTopic(Id threadId, string newTopic)
    {
        ThreadId = threadId;
        NewTopic = newTopic;
    }

    public Id ThreadId { get; }
    public string NewTopic { get; }
}

namespace Realizer.Messages.Conversations;

public class NewThread : IHttpCommand
{
    public NewThread(string threadName, string threadId)
    {
        ThreadName = threadName;
        ThreadId = threadId;
        Id.TryFromAny(threadId, out var id);
        TotemThreadId = id;
    }

    public string ThreadName { get; }
    public string ThreadId { get; }
    public Id TotemThreadId { get; }
}

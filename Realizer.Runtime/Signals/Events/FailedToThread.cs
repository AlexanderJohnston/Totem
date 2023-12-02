namespace Realizer.Runtime.Signals.Events;
public class FailedToThread : IEvent
{
    // Take in the memory id, thread id, and exception text
    public FailedToThread(Id threadId, string exception)
    {
        ThreadId = threadId;
        ExceptionMsg = exception;
    }

    public Id ThreadId { get; }
    public string ExceptionMsg { get; }
}

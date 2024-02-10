using Realizer.Messages.Conversations;
using Realizer.Runtime.Signals.Events.Conversation;

namespace Realizer.Runtime.Signals.Reports;

public class ThreadReport : Report<ThreadRow>
{
    public static Id Route(ConsiderReply e) => e.ThreadId;

    public void When(ConsiderReply e)
    {
        Row.Reply = e.Message;
    }
}

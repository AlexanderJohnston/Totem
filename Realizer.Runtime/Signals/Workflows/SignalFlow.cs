using Realizer.Messages.Signals;
using Realizer.Runtime.Signals.Events;

namespace Realizer.Runtime.Signals.Workflows;
public class SignalFlow : Workflow
{
    public static Id Route(SignalThreaded e) => e.Message.TotemThreadId;

    public void When(SignalThreaded e) =>
        ThenEnqueue(new IdentifySignal(e.ThreadId, e.Message));

}

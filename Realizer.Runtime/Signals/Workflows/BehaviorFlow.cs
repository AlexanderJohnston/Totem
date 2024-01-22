using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realizer.Messages.Conversations;
using Realizer.Runtime.Signals.Events;
using Realizer.Runtime.Signals.Events.Conversation;

namespace Realizer.Runtime.Signals.Workflows;
public class BehaviorFlow : Workflow
{
    public static Id Route(EnterThoughtLoop e) => e.Message.TotemThreadId;

    public void When(EnterThoughtLoop e) =>
    ThenEnqueue(new Consider(e.ThreadId, e.Message, e.State));


    //public static Id Route(ProgressThought e) => e.ThreadId;

    //public static Id Route(DetermineConversation e) => e.ThreadId;
}

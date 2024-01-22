using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realizer.Messages.Conversations;
using Realizer.Messages.Signals;
using Realizer.Runtime.Signals.Events;
using Realizer.Runtime.Signals.Events.Conversation;

namespace Realizer.Runtime.Signals.Workflows;
public class ConversationFlow : Workflow
{
    // Route events for ConversationStarted by ThreadId
    //public static Id Route(ConversationStarted e) => e.ThreadId;

    //// Route events for ConversationEnded by ThreadId
    //public static Id Route(ConversationEnded e) => e.ThreadId;

    public static Id Route(UserMessageIdentified e) => e.ThreadId;
    public void When(UserMessageIdentified e) =>
        ThenEnqueue(new AnalyzeUserMessage(e.ThreadId, e.Signal));


    // Handle ConversationStarted event and enqueue a StartConversation command
    //public void When(ConversationStarted e) =>
    //    ThenEnqueue(new StartConversation(e.ThreadId, e.UserId, e.Message));

    //// Handle ConversationEnded event and enqueue an EndConversation command
    //public void When(ConversationEnded e) =>
    //    ThenEnqueue(new EndConversation(e.ThreadId, e.UserId));

    //// Handle ContextualConversation event and enqueue an AnalyzeUserMessage command
    //public void When(ContextualConversation e) =>
    //    ThenEnqueue(new AnalyzeUserMessage(e.MemoryId, e.Message));
}

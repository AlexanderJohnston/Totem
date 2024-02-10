using Realizer.Messages.Conversations;
using Realizer.Runtime.Signals.Events;
using Realizer.Runtime.Signals.Events.Conversation;

namespace Realizer.Runtime.Signals.Topics;
// Topic Class Definition
public class ConversationTopic : Topic
{
    string _currentTopic;
    List<Id> _currentUsers;
    ConversationState _currentState;
    ThinkingState _thinkingState;
    Id _threadId;

    public static Id Route(AnalyzeUserMessage command) => command.ThreadId;

    public void Given(EnterThoughtLoop e)
    {
        if (_currentState == ConversationState.NotStarted)
        {
            _threadId = e.ThreadId;
            _currentState = ConversationState.Started;
        }
    }

    // Command Routing - Add AnalyzeUserMessage and ConversationAnalyzed routing
    // Additional Command Handlers:

    public async Task When(AnalyzeUserMessage command, CancellationToken cancel)
    {
        // Emit event with the current state and the DiscordMessage
        if (_currentState == ConversationState.NotStarted)
        {
            Then(new EnterThoughtLoop(command.ThreadId, command.Signal, _currentState));
        }
        else
        {
            Then(new EnterThoughtLoop(command.ThreadId, command.Signal, _currentState));
        }
    }
}

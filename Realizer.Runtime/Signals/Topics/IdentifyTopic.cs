using Realizer.Messages.Signals;
using Realizer.Runtime.Signals.Events;
using REBL;

namespace Realizer.Runtime.Signals.Topics;
public class IdentifyTopic : Topic
{
    public static Id Route(IdentifySignal command) => command.Signal.TotemThreadId;

    public IdentifyTopic(REBLConsole console)
    {
        _console = console;
    }

    REBLConsole _console;

    public async Task When(IdentifySignal command, CancellationToken cancellationToken)
    {
        try
        {
            var potentialCommandResult = await _console.RunHeadless(null, command.Signal.Message);
            if(!string.IsNullOrEmpty(potentialCommandResult))
            {
                Then(new CommandIdentified(command.MemoryId, potentialCommandResult));
            }
            else
            {
                Then(new UserMessageIdentified(command.MemoryId, command.Signal));
            }
        }
        catch(Exception exception)
        {
            Then(new FailedToThread(Id.From(command.Signal.TotemThreadId), exception.ToString()));
        }
    }
}

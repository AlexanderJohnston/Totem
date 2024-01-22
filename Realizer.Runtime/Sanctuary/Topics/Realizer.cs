using Realizer.Messages.Conversations;
using Realizer.Messages.Sanctuary;
using Realizer.Runtime.Sanctuary.Events.REBL;
using Realizer.Runtime.Signals.Events;
using Realizer.Runtime.Signals.Events.Conversation;
using REBL;
using REBL.Tests;

namespace Realizer.Runtime.Sanctuary.Topics;

public class Realizer : Topic
{
    public static Id Route(Rebel command) => command.InstanceId;

    public static Id Route(EnterThoughtLoop command) => command.ThreadId;
    public static Id Route(Consider command) => command.ThreadId;

    public static Id Route(NewThread command) => command.TotemThreadId;


    public Realizer(/*REBLConsole console*/)
    {
        //_console = console;
    }

    REBLConsole _console;

    public void Given(ConfigureThread e)
    {
        _console = new REBLConsole();
    }

    public async Task When(NewThread thread, CancellationToken cancellationToken)
    {
        Then(new ConfigureThread());
    }

    public async Task When(Consider consider, CancellationToken cancellationToken)
    {
        var command = TesterHelper.GetInput(ref _console, consider.Message.Message);
        string potentialCommandResult = await _console.RunHeadless(command);

        // make template
        command = TesterHelper.MakeEchoTemplate(ref _console, "user.input.template");
        potentialCommandResult = await _console.RunHeadless(command);

        // make buffer user.input.buffer
        command = TesterHelper.MakeBuffer(ref _console, "user.input.buffer");
        potentialCommandResult = await _console.RunHeadless(command);

        // addtemplate to the buffer
        command = TesterHelper.AddTemplate(ref _console, "user.input.buffer", "user.input.template");
        potentialCommandResult = await _console.RunHeadless(command);


        // add the user.input expression to the buffer
        command = TesterHelper.AddExpression(ref _console, "user.input.buffer", "user.input");
        potentialCommandResult = await _console.RunHeadless(command);

        // make a claim based on buffer
        command = TesterHelper.MakeCLaim(ref _console, "user.input.buffer");
        potentialCommandResult = await _console.RunHeadless(command);
    }

    public async Task When(Rebel rebel, CancellationToken cancellationToken)
    {
        try
        {
            string potentialCommandResult = await _console.RunHeadless(null, rebel.Command);
            if(!string.IsNullOrEmpty(potentialCommandResult))
            {
                Then(new ReblUpdated(rebel.InstanceId, potentialCommandResult));
            }
            else
            {
                Then(new ReblCommandUnknown(rebel.Instance, rebel.InstanceId));
            }
        }
        catch(Exception exception)
        {
            Then(new ReblCommandFailed(rebel.Command, exception.ToString()));
        }
    }
}

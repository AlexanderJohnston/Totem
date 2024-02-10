using Realizer.Messages.Conversations;
using Realizer.Messages.Sanctuary;
using Realizer.Runtime.Sanctuary.Events.REBL;
using Realizer.Runtime.Signals.Events;
using Realizer.Runtime.Signals.Events.Conversation;
using Realizer.Services.Signals;
using REBL;
using REBL.Tests;

namespace Realizer.Runtime.Sanctuary.Topics;

public class Realizer : Topic
{
    public static Id Route(Rebel command) => command.InstanceId;

    public static Id Route(EnterThoughtLoop command) => command.ThreadId;
    public static Id Route(Consider command) => command.ThreadId;

    public static Id Route(NewThread command) => command.TotemThreadId;


    public Realizer(RebelService rebelService)
    {
        _rebelService = rebelService;
    }

    RebelService _rebelService;

    public void Given(ConfigureThread e)
    {
        _rebelService.NewThread();
    }

    public async Task When(NewThread thread, CancellationToken cancellationToken)
    {
        Then(new ConfigureThread());
    }

    public async Task When(Consider choice, CancellationToken cancellationToken)
    {
        var result = _rebelService.GetInput(choice.Signal.Message);

        // Make a template which echoes the user input to elicit a reaction
        result = _rebelService.MakeTemplate("user.input.template", "{0}");

        // Make a buffer containing the user's input
        result = _rebelService.MakeBuffer("user.input.buffer");

        // Add the echo template to the buffer
        result = _rebelService.AddTemplate("user.input.template", "user.input.buffer");

        // Add the user.input expression to the buffer
        result = _rebelService.AddExpression("user.input", "user.input.buffer");

        // Realize the buffer as a Claim
        result = _rebelService.MakeClaim("user.input.buffer");

        // Check if thought was successfully claimed and echoed back from the REBL
        if (result == choice.Signal.Message)
        {
            // Elicit AI behavior based on the claim
            var response = _rebelService.RunHeadless("run user.input.buffer");

            // Report the result of AI behavior
            Then(new ConsiderReply(response, choice.ThreadId, choice.Signal.ThreadId));
        }
    }

    public async Task When(Rebel rebel, CancellationToken cancellationToken)
    {
        try
        {
            string potentialCommandResult = _rebelService.RunHeadless(rebel.Command);
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

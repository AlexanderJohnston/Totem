using Memory.Converse;
using Realizer.Messages.Signals;
using Realizer.Runtime.Signals.Events;
using Realizer.Services.Signals;

namespace Realizer.Runtime.Signals.Topics;
public sealed class ThreadTopic : Topic
{
    public static Id Route(SignalThread command) => command.TotemThreadId;

    readonly IShortTermMemory<string> _memories;
    readonly IMultiTask _conversations;

    public ThreadTopic(IMultiTask service, IShortTermMemory<string> memories)
    {
        _memories = memories;
        _conversations = service;
    }

    public async Task When(SignalThread command, CancellationToken cancellationToken)
    {
        try
        {
            var memoryId = _memories.Remember(command.Message, command.DiscriminatorValue, command.UserName, MemoryType.Unknown, command.ThreadId);
            var message = new DiscordMessage(channelId: command.ChannelId,
                                             threadid: command.ThreadId,
                                             message: command.Message,
                                             context: command.Context,
                                             topic: command.Topic,
                                             source: command.Source,
                                             userName: command.UserName,
                                             discriminatorValue: command.DiscriminatorValue);

            Then(new SignalThreaded(command.TotemThreadId, message));
        }
        catch(Exception exception)
        {
            Then(new FailedToThread(command.TotemThreadId, exception.ToString()));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Realizer.Messages.Signals;
public sealed class SignalThread : IHttpCommand
{
    public SignalThread(string userName, ushort discriminatorValue, string message, string context, string topic, ulong source, string threadId, ulong channelId)
    {
        UserName = userName;
        DiscriminatorValue = discriminatorValue;
        Message = message;
        Context = context;
        Source = source;
        Topic = topic;
        ThreadId = threadId;
        ChannelId = channelId;
        Id.TryFromAny(threadId, out var id);
        TotemThreadId = id;
    }

    public string UserName { get; }
    public ushort DiscriminatorValue { get; }
    public string Message { get; }
    public string Context { get; }
    public ulong Source { get; }
    public string Topic { get; }
    public string ThreadId { get; }
    public ulong ChannelId { get; }
    public Id TotemThreadId { get; }
}

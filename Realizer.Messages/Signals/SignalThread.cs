using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Realizer.Messages.Signals;
public sealed class SignalThread : IHttpCommand
{
    public SignalThread(IUser user, string message, string context, string topic, ulong source, ushort threadid, ulong channelId)
    {
        User = user;
        ThreadId = threadid;
        Message = message;
        Context = context;
        Source = source;
        Topic = topic;
        ChannelId = channelId;
    }

    public IUser User { get; }
    public string Message { get; }
    public string Context { get; }
    public ulong Source { get; }
    public string Topic { get; }
    public ushort ThreadId { get; }
    public ulong ChannelId { get; }
    public Id TotemThreadId { get; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realizer.Runtime.Signals;
public class DiscordMessage
{
    public DiscordMessage(string userName, ushort discriminatorValue, string message, string context, string topic, ulong source, ushort threadid, ulong channelId)
    {
        UserName = userName;
        DiscriminatorValue = discriminatorValue;
        Message = message;
        Context = context;
        Source = source;
        Topic = topic;
        ThreadId = threadid;
        ChannelId = channelId;
        TotemThreadId = Id.From(threadid);
    }
    public string UserName { get; }
    public ushort DiscriminatorValue { get; }
    public string Message { get; }
    public string Context { get; }
    public ulong Source { get; }
    public string Topic { get; }
    public ushort ThreadId { get; }
    public ulong ChannelId { get; }
    public Id TotemThreadId { get; }
}

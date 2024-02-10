namespace Realizer.Runtime.Signals.Events.Conversation;
public class ConsiderReply : IEvent
{
    public ConsiderReply(string message, Id threadId, ushort discordId)
    {
        Message = message;
        ThreadId = threadId;
        DiscordId = discordId;
    }
    public string Message { get; set; }
    public Id ThreadId { get; set; }

    public ushort DiscordId { get; set; }
}

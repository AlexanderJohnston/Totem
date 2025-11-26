namespace Totem.Events;

public sealed class EventEnvelope : MessageEnvelope
{
    public EventEnvelope(IEvent e, TimelineKey topicKey, DateTimeOffset whenOccurred, EnvelopeInfo info) : base(info)
    {
        Event = e;
        EventInfo = EventInfo.From(e.GetType());
        TopicKey = topicKey;
        WhenOccurred = whenOccurred;
    }

    public EventEnvelope(IEvent e, TimelineKey topicKey, DateTimeOffset whenOccurred, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(e, topicKey, whenOccurred, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IEvent Event { get; }
    public EventInfo EventInfo { get; }
    public Type EventType => EventInfo.DeclaredType;
    public Id EventId => Info.MessageId;
    public TimelineKey TopicKey { get; }
    public DateTimeOffset WhenOccurred { get; }
}

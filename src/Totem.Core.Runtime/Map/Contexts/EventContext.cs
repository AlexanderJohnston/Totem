namespace Totem.Map.Contexts;

internal sealed class EventContext<TEvent> : MessageContext, IEventContext<TEvent>
    where TEvent : IEvent
{
    internal EventContext(EventEnvelope envelope, EventType eventType) : base(envelope)
    {
        Event = (TEvent) Envelope.Event;
        EventType = eventType;
    }

    public new EventEnvelope Envelope => (EventEnvelope) base.Envelope;
    public TEvent Event { get; }
    public EventInfo EventInfo => Envelope.EventInfo;
    public EventType EventType { get; }
    public Id EventId => Envelope.EventId;
    public TimelineKey TopicKey => Envelope.TopicKey;
    public DateTimeOffset WhenOccurred => Envelope.WhenOccurred;
}

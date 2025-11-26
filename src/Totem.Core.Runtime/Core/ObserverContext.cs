namespace Totem.Core;

public abstract class ObserverContext<TEvent> : MessageContext, IEventContext<TEvent>
    where TEvent : IEvent
{
    internal ObserverContext(EventEnvelope envelope, EventType eventType, Observation observation) : base(envelope)
    {
        Event = (TEvent) envelope.Event;
        EventType = eventType;
        Observation = observation;
    }

    public new EventEnvelope Envelope => (EventEnvelope) base.Envelope;
    public TEvent Event { get; }
    public EventInfo EventInfo => Envelope.EventInfo;
    public EventType EventType { get; }
    public Id EventId => Envelope.EventId;
    public TimelineKey TopicKey => Envelope.TopicKey;
    public DateTimeOffset WhenOccurred => Envelope.WhenOccurred;
    public Observation Observation { get; }
}

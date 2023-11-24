namespace Totem.Map.Contexts;

internal sealed class EventHandlerContext<TEvent> : MessageContext, IEventHandlerContext<TEvent>
    where TEvent : IEvent
{
    internal EventHandlerContext(EventEnvelope envelope, EventType eventType) : base(envelope)
    {
        Event = (TEvent) envelope.Event;
        EventType = eventType;
    }

    public new EventEnvelope Envelope => (EventEnvelope) base.Envelope;
    public TEvent Event { get; }
    public EventInfo EventInfo => Envelope.EventInfo;
    public EventType EventType { get; }
    public Id EventId => Envelope.EventId;
    public TimelineKey TopicKey => Envelope.TopicKey;
    public DateTimeOffset WhenOccurred => Envelope.WhenOccurred;
    public EventHandlerType HandlerType => EventType.Handler!;

    public override string ToString() =>
        $"{base.ToString()} => {HandlerType}";
}

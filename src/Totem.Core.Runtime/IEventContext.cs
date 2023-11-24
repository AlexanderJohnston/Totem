namespace Totem;

public interface IEventContext<out TEvent> : IMessageContext
    where TEvent : IEvent
{
    new EventEnvelope Envelope { get; }
    TEvent Event { get; }
    EventInfo EventInfo { get; }
    EventType EventType { get; }
    Id EventId { get; }
    TimelineKey TopicKey { get; }
    DateTimeOffset WhenOccurred { get; }
}

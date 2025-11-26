namespace Totem;

public interface IObserverContext<out TEvent> : IEventContext<TEvent>
    where TEvent : IEvent
{
    Observation Observation { get; }
}

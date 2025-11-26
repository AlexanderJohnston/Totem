namespace Totem;

public interface IEventHandlerContext<out TEvent> : IEventContext<TEvent>
    where TEvent : IEvent
{
    EventHandlerType HandlerType { get; }
}

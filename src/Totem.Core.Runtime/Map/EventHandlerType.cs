namespace Totem.Map;

public sealed class EventHandlerType : MessageHandlerType
{
    internal EventHandlerType(Type declaredType, Type serviceType, EventType e) : base(declaredType, serviceType) =>
        Event = e;

    public EventType Event { get; }
}

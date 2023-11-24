namespace Totem.Map.Builder;

internal sealed class EventHandlerBuilder : RuntimeTypeBuilder<EventHandlerType>
{
    internal EventHandlerBuilder(Type declaredType) : base(declaredType)
    { }

    internal EventBuilder Event { get; private set; } = null!;
    internal Type ServiceType { get; private set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var eventType = DeclaredType.GetImplementedInterfaceGenericArguments(typeof(IEventHandler<>)).Single();

        if(!map.Messages.Events.TryGet(eventType, out var e))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        if(e.Handler is not null)
        {
            SetError(BuildErrors.HandlerDuplicated, new { e.Handler });
            return;
        }

        Event = e;
        Event.Handler = this;
        ServiceType = typeof(IEventHandler<>).MakeGenericType(eventType);
    }

    internal override void Validate()
    {
        if(Event.HasError)
        {
            SetError(BuildErrors.HandlerMessageHasError);
        }
    }

    protected override EventHandlerType BuildValue()
    {
        var handler = new EventHandlerType(DeclaredType, ServiceType, Event.Value);

        Event.Value.Handler = handler;

        return handler;
    }
}

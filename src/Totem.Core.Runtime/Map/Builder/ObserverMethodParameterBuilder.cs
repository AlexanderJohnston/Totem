namespace Totem.Map.Builder;

internal sealed class ObserverMethodParameterBuilder : RuntimeMethodParameterBuilder<EventParameter>
{
    readonly Type? _extendedContextType;

    internal ObserverMethodParameterBuilder(ParameterInfo parameter, Type? extendedContextType = null) : base(parameter) =>
        _extendedContextType = extendedContextType;

    internal EventBuilder Event { get; private set; } = null!;
    internal bool HasContext { get; private set; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var type = Info.ParameterType;
        var isEvent = typeof(IEvent).IsAssignableFrom(type);

        Type? eventType;

        if(isEvent)
        {
            eventType = type;
        }
        else
        {
            eventType = type.GetImplementedInterfaceGenericArguments(typeof(IEventContext<>)).SingleOrDefault();

            if(eventType is null && _extendedContextType is not null)
            {
                eventType = type.GetImplementedInterfaceGenericArguments(_extendedContextType).SingleOrDefault();
            }
        }

        if(eventType is null)
        {
            SetError(BuildErrors.RuntimeMethodParameterNotMessageOrContext);
            return;
        }

        if(!map.Messages.Events.TryGet(eventType, out var e))
        {
            SetError(BuildErrors.EventNotFound);
            return;
        }

        Event = e;
        HasContext = !isEvent;
    }

    protected override EventParameter BuildValue() =>
        new(Info, Event.Value, HasContext);
}

namespace Totem.Map.Contexts;

internal sealed class EventHandlerContextFactory
{
    delegate IEventHandlerContext<IEvent> CompiledFactory(EventEnvelope envelope);

    readonly ConcurrentDictionary<EventType, CompiledFactory> _factoriesByEventType = new();
    readonly RuntimeMap _map;

    internal EventHandlerContextFactory(RuntimeMap map) =>
        _map = map;

    internal IEventHandlerContext<IEvent> Create(EventEnvelope envelope)
    {
        if(!_map.Events.TryGet(envelope.EventType, out var eventType))
            throw new ArgumentException($"Expected known event of type {envelope.EventType}", nameof(envelope));

        var factory = _factoriesByEventType.GetOrAdd(eventType, CompileFactory);

        return factory(envelope);
    }

    CompiledFactory CompileFactory(EventType eventType)
    {
        // envelope => new EventHandlerContext<TEvent>(envelope, eventType)

        var constructor = typeof(EventHandlerContext<>)
            .MakeGenericType(eventType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(EventEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(eventType));
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}

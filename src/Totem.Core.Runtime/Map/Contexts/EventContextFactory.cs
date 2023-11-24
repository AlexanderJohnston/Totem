namespace Totem.Map.Contexts;

internal sealed class EventContextFactory
{
    delegate IEventContext<IEvent> CompiledFactory(EventEnvelope envelope);

    readonly ConcurrentDictionary<EventType, CompiledFactory> _factoriesByEventType = new();
    readonly RuntimeMap _map;

    internal EventContextFactory(RuntimeMap map) =>
        _map = map;

    internal IEventContext<IEvent> Create(EventEnvelope envelope)
    {
        if(!_map.Events.TryGet(envelope.EventType, out var eventType))
            throw new ArgumentException($"Expected known event of type {envelope.EventType}", nameof(envelope));

        var factory = _factoriesByEventType.GetOrAdd(eventType, CompileFactory);

        return factory(envelope);
    }

    CompiledFactory CompileFactory(EventType eventType)
    {
        // envelope => new EventContext<TEvent>(envelope, eventType)

        var constructor = typeof(EventContext<>)
            .MakeGenericType(eventType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(EventEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(eventType));
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}

namespace Totem.Map.Contexts;

internal sealed class WorkflowContextFactory
{
    delegate IWorkflowContext<IEvent> CompiledFactory(EventEnvelope envelope, ObserverRoute route);

    readonly ConcurrentDictionary<Type, CompiledFactory> _factoriesByEventType = new();
    readonly RuntimeMap _map;

    internal WorkflowContextFactory(RuntimeMap map) =>
        _map = map;

    internal IWorkflowContext<IEvent> Create(EventEnvelope envelope, ObserverRoute route)
    {
        var factory = _factoriesByEventType.GetOrAdd(envelope.EventType, CompileFactory);

        return factory(envelope, route);
    }

    CompiledFactory CompileFactory(Type eventType)
    {
        if(!_map.Events.TryGet(eventType, out var mapType))
            throw new ArgumentException($"Expected mapped event of type {eventType}", nameof(eventType));

        // (envelope, route) => new WorkflowContext<TEvent>(envelope, mapType, route)

        var constructor = typeof(WorkflowContext<>)
            .MakeGenericType(eventType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(EventEnvelope), "envelope");
        var routeParameter = Expression.Parameter(typeof(ObserverRoute), "route");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(mapType), routeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter, routeParameter);

        return lambda.Compile();
    }
}

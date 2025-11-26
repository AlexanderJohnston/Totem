namespace Totem.Map.Contexts;

internal sealed class SubscriptionContextFactory
{
    delegate ISubscriptionContext<ISubscription> CompiledFactory(SubscriptionEnvelope envelope);

    readonly ConcurrentDictionary<SubscriptionType, CompiledFactory> _factoriesBySubscriptionType = new();
    readonly RuntimeMap _map;

    internal SubscriptionContextFactory(RuntimeMap map) =>
        _map = map;

    internal ISubscriptionContext<ISubscription> Create(SubscriptionEnvelope envelope)
    {
        if(!_map.Subscriptions.TryGet(envelope.SubscriptionType, out var subscriptionType))
            throw new ArgumentException($"Expected mapped subscription of type {envelope.SubscriptionType}", nameof(envelope));

        var factory = _factoriesBySubscriptionType.GetOrAdd(subscriptionType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(SubscriptionType subscriptionType)
    {
        // envelope => new SubscriptionContext<TSubscription>(envelope, subscriptionType)

        var constructor = typeof(SubscriptionContext<>)
            .MakeGenericType(subscriptionType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(SubscriptionEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(subscriptionType));
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}

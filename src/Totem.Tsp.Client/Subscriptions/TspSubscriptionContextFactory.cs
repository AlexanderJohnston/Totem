namespace Totem.Subscriptions;

internal sealed class TspSubscriptionContextFactory
{
    delegate ITspSubscriptionContext<ITspSubscription> CompiledFactory(TspSubscriptionEnvelope envelope);

    readonly ConcurrentDictionary<Type, CompiledFactory> _factoriesByCommandType = new();

    internal ITspSubscriptionContext<ITspSubscription> Create(TspSubscriptionEnvelope envelope)
    {
        var factory = _factoriesByCommandType.GetOrAdd(envelope.SubscriptionType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(Type commandType)
    {
        // envelope => new TspSubscriptionContext<TCommand>(envelope)

        var constructor = typeof(TspSubscriptionContext<>)
            .MakeGenericType(commandType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(TspSubscriptionEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}

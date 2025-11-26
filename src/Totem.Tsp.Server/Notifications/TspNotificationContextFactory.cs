namespace Totem.Notifications;

internal sealed class TspNotificationContextFactory
{
    delegate ITspNotificationContext<ITspNotification> CompiledFactory(TspNotificationEnvelope envelope);

    readonly ConcurrentDictionary<Type, CompiledFactory> _factoriesByCommandType = new();

    internal ITspNotificationContext<ITspNotification> Create(TspNotificationEnvelope envelope)
    {
        var factory = _factoriesByCommandType.GetOrAdd(envelope.NotificationType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(Type commandType)
    {
        // envelope => new TspNotificationContext<TCommand>(envelope)

        var constructor = typeof(TspNotificationContext<>)
            .MakeGenericType(commandType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(TspNotificationEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}

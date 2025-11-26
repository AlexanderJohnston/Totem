namespace Totem.Map.Contexts;

internal sealed class NotificationContextFactory
{
    delegate INotificationContext<INotification> CompiledFactory(NotificationEnvelope envelope);

    readonly ConcurrentDictionary<NotificationType, CompiledFactory> _factoriesByNotificationType = new();
    readonly RuntimeMap _map;

    internal NotificationContextFactory(RuntimeMap map) =>
        _map = map;

    internal INotificationContext<INotification> Create(NotificationEnvelope envelope)
    {
        if(!_map.Notifications.TryGet(envelope.NotificationType, out var notificationType))
            throw new ArgumentException($"Expected mapped notification of type {envelope.NotificationType}", nameof(envelope));

        var factory = _factoriesByNotificationType.GetOrAdd(notificationType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(NotificationType notificationType)
    {
        // envelope => new NotificationContext<TNotification>(envelope, notificationType)

        var constructor = typeof(NotificationContext<>)
            .MakeGenericType(notificationType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(NotificationEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(notificationType));
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}

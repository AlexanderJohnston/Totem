namespace Totem.Notifications;

public sealed class NotificationHandlerMiddleware : INotificationMiddleware
{
    delegate Task CompiledHandler(INotificationContext<INotification> context, CancellationToken cancellationToken);

    readonly ConcurrentDictionary<NotificationType, CompiledHandler> _handlersByNotification = new();
    readonly IServiceProvider _services;

    public NotificationHandlerMiddleware(IServiceProvider services) =>
        _services = services;

    public async Task InvokeAsync(INotificationContext<INotification> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var handler = _handlersByNotification.GetOrAdd(context.NotificationType, CompileHandler);

        await handler(context, cancellationToken);

        context.ExpectNoErrors();

        await next();
    }

    CompiledHandler CompileHandler(NotificationType notification)
    {
        // (context, cancellationToken) => HandleAsync<TNotification>(context, cancellationToken)

        var contextParameter = Expression.Parameter(typeof(IEventContext<IEvent>), "context");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var callHandle = Expression.Call(
            Expression.Constant(this),
            nameof(HandleAsync),
            new[] { notification.DeclaredType },
            contextParameter,
            cancellationTokenParameter);

        var lambda = Expression.Lambda<CompiledHandler>(callHandle, contextParameter, cancellationTokenParameter);

        return lambda.Compile();
    }

    async Task HandleAsync<TNotification>(INotificationContext<INotification> context, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INotificationHandler<TNotification>>();

        await handler.HandleAsync((INotificationContext<TNotification>) context, cancellationToken);
    }
}

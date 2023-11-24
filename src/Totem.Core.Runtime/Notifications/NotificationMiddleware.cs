namespace Totem.Notifications;

public sealed class NotificationMiddleware : INotificationMiddleware
{
    readonly Func<INotificationContext<INotification>, Func<Task>, CancellationToken, Task> _middleware;

    public NotificationMiddleware(Func<INotificationContext<INotification>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(INotificationContext<INotification> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class NotificationMiddleware<TService> : INotificationMiddleware
    where TService : INotificationMiddleware
{
    readonly IServiceProvider _services;

    public NotificationMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(INotificationContext<INotification> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

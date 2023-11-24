namespace Totem.Notifications;

public sealed class TspNotificationMiddleware : ITspNotificationMiddleware
{
    readonly Func<ITspNotificationContext<ITspNotification>, Func<Task>, CancellationToken, Task> _middleware;

    public TspNotificationMiddleware(Func<ITspNotificationContext<ITspNotification>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(ITspNotificationContext<ITspNotification> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class TspServerNotificationMiddleware<TService> : ITspNotificationMiddleware
    where TService : ITspNotificationMiddleware
{
    readonly IServiceProvider _services;

    public TspServerNotificationMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(ITspNotificationContext<ITspNotification> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

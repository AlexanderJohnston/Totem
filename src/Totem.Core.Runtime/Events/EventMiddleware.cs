namespace Totem.Events;

public sealed class EventMiddleware : IEventMiddleware
{
    readonly Func<IEventContext<IEvent>, Func<Task>, CancellationToken, Task> _middleware;

    public EventMiddleware(Func<IEventContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IEventContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class EventMiddleware<TService> : IEventMiddleware
    where TService : IEventMiddleware
{
    readonly IServiceProvider _services;

    public EventMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IEventContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

namespace Totem.Events;

public sealed class EventHandlerMiddleware : IEventHandlerMiddleware
{
    readonly Func<IEventHandlerContext<IEvent>, Func<Task>, CancellationToken, Task> _middleware;

    public EventHandlerMiddleware(Func<IEventHandlerContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IEventHandlerContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class EventHandlerMiddleware<TService> : IEventHandlerMiddleware
    where TService : IEventHandlerMiddleware
{
    readonly IServiceProvider _services;

    public EventHandlerMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IEventHandlerContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

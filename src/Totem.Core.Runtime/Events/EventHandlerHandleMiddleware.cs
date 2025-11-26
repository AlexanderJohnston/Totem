namespace Totem.Events;

public sealed class EventHandlerHandleMiddleware : IEventHandlerMiddleware
{
    delegate Task CompiledHandler(IEventHandlerContext<IEvent> context, CancellationToken cancellationToken);

    readonly ConcurrentDictionary<EventType, CompiledHandler> _handlersByEvent = new();
    readonly IServiceProvider _services;

    public EventHandlerHandleMiddleware(IServiceProvider services) =>
        _services = services;

    public async Task InvokeAsync(IEventHandlerContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var handler = _handlersByEvent.GetOrAdd(context.EventType, CompileHandler);

        await handler(context, cancellationToken);

        context.ExpectNoErrors();

        await next();
    }

    CompiledHandler CompileHandler(EventType e)
    {
        // (context, cancellationToken) => HandleAsync<TEvent>(context, cancellationToken)

        var contextParameter = Expression.Parameter(typeof(IEventHandlerContext<IEvent>), "context");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var callHandle = Expression.Call(
            Expression.Constant(this),
            nameof(HandleAsync),
            new[] { e.DeclaredType },
            contextParameter,
            cancellationTokenParameter);

        var lambda = Expression.Lambda<CompiledHandler>(callHandle, contextParameter, cancellationTokenParameter);

        return lambda.Compile();
    }

    async Task HandleAsync<TEvent>(IEventHandlerContext<IEvent> context, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<TEvent>>();

        await handler.HandleAsync((IEventHandlerContext<TEvent>) context, cancellationToken);
    }
}

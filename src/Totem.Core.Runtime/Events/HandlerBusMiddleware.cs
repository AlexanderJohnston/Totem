namespace Totem.Events;

public sealed class HandlerBusMiddleware : IEventMiddleware
{
    readonly IHandlerBus _bus;

    public HandlerBusMiddleware(IHandlerBus bus) =>
        _bus = bus;

    public async Task InvokeAsync(IEventContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken)
    {
        if(context.EventType.Handler is not null)
        {
            await _bus.PublishAsync(context.Envelope, cancellationToken);
        }

        await next();
    }
}

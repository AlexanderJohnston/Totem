namespace Totem.Events;

public interface IEventHandlerMiddleware
{
    Task InvokeAsync(IEventHandlerContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken);
}

namespace Totem.Events;

public sealed class WorkflowBusMiddleware : IEventMiddleware
{
    readonly IWorkflowBus _bus;

    public WorkflowBusMiddleware(IWorkflowBus bus) =>
        _bus = bus;

    public async Task InvokeAsync(IEventContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var routes = context.EventType.RouteWorkflows(context).ToList();

        if(routes.Any())
        {
            await _bus.PublishAsync(context.Envelope, routes, cancellationToken);
        }

        await next();
    }
}

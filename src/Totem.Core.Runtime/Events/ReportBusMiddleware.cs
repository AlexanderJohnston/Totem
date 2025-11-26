namespace Totem.Events;

public sealed class ReportBusMiddleware : IEventMiddleware
{
    readonly IReportBus _bus;

    public ReportBusMiddleware(IReportBus bus) =>
        _bus = bus;

    public async Task InvokeAsync(IEventContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var routes = context.EventType.RouteReports(context).ToList();

        if(routes.Any())
        {
            await _bus.PublishAsync(context.Envelope, routes, cancellationToken);
        }

        await next();
    }
}

namespace Totem.Reports;

public sealed class ReportMiddleware : IReportMiddleware
{
    readonly Func<IReportContext<IEvent>, Func<Task>, CancellationToken, Task> _middleware;

    public ReportMiddleware(Func<IReportContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IReportContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class ReportMiddleware<TService> : IReportMiddleware
    where TService : IReportMiddleware
{
    readonly IServiceProvider _services;

    public ReportMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IReportContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

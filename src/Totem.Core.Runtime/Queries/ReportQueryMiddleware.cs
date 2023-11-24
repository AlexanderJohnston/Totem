namespace Totem.Queries;

public sealed class ReportQueryMiddleware : IReportQueryMiddleware
{
    readonly Func<IReportQueryContext<IReportQuery>, Func<Task>, CancellationToken, Task> _middleware;

    public ReportQueryMiddleware(Func<IReportQueryContext<IReportQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IReportQueryContext<IReportQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class RowQueryMiddleware<TService> : IReportQueryMiddleware
    where TService : IReportQueryMiddleware
{
    readonly IServiceProvider _services;

    public RowQueryMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IReportQueryContext<IReportQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

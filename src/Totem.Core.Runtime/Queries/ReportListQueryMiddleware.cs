namespace Totem.Queries;

public sealed class ReportListQueryMiddleware : IReportListQueryMiddleware
{
    readonly Func<IReportListQueryContext<IReportListQuery>, Func<Task>, CancellationToken, Task> _middleware;

    public ReportListQueryMiddleware(Func<IReportListQueryContext<IReportListQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IReportListQueryContext<IReportListQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class ListQueryMiddleware<TService> : IReportListQueryMiddleware
    where TService : IReportListQueryMiddleware
{
    readonly IServiceProvider _services;

    public ListQueryMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IReportListQueryContext<IReportListQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

namespace Totem.Queries;

public sealed class HttpReportListQueryMiddleware : IHttpReportListQueryMiddleware
{
    readonly Func<IHttpReportListQueryContext<IHttpReportListQuery>, Func<Task>, CancellationToken, Task> _middleware;

    public HttpReportListQueryMiddleware(Func<IHttpReportListQueryContext<IHttpReportListQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IHttpReportListQueryContext<IHttpReportListQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class HttpReportListQueryMiddleware<TService> : IHttpReportListQueryMiddleware
    where TService : IHttpReportListQueryMiddleware
{
    readonly IServiceProvider _services;

    public HttpReportListQueryMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IHttpReportListQueryContext<IHttpReportListQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

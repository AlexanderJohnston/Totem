namespace Totem.Queries;

public sealed class HttpReportQueryMiddleware : IHttpReportQueryMiddleware
{
    readonly Func<IHttpReportQueryContext<IHttpReportQuery>, Func<Task>, CancellationToken, Task> _middleware;

    public HttpReportQueryMiddleware(Func<IHttpReportQueryContext<IHttpReportQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IHttpReportQueryContext<IHttpReportQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class HttpReportQueryMiddleware<TService> : IHttpReportQueryMiddleware
    where TService : IHttpReportQueryMiddleware
{
    readonly IServiceProvider _services;

    public HttpReportQueryMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IHttpReportQueryContext<IHttpReportQuery> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

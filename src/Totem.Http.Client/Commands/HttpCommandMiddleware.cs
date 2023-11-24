namespace Totem.Commands;

public sealed class HttpCommandMiddleware : IHttpCommandMiddleware
{
    readonly Func<IHttpCommandContext<IHttpCommand>, Func<Task>, CancellationToken, Task> _middleware;

    public HttpCommandMiddleware(Func<IHttpCommandContext<IHttpCommand>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IHttpCommandContext<IHttpCommand> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class HttpCommandMiddleware<TService> : IHttpCommandMiddleware
    where TService : IHttpCommandMiddleware
{
    readonly IServiceProvider _services;

    public HttpCommandMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IHttpCommandContext<IHttpCommand> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

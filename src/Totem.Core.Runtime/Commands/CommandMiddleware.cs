namespace Totem.Commands;

public sealed class CommandMiddleware : ICommandMiddleware
{
    readonly Func<ICommandContext<ICommand>, Func<Task>, CancellationToken, Task> _middleware;

    public CommandMiddleware(Func<ICommandContext<ICommand>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(ICommandContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class CommandMiddleware<TService> : ICommandMiddleware
    where TService : ICommandMiddleware
{
    readonly IServiceProvider _services;

    public CommandMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(ICommandContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

namespace Totem.Topics;

public sealed class TopicMiddleware : ITopicMiddleware
{
    readonly Func<ITopicContext<ICommand>, Func<Task>, CancellationToken, Task> _middleware;

    public TopicMiddleware(Func<ITopicContext<ICommand>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(ITopicContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class TopicMiddleware<TService> : ITopicMiddleware
    where TService : ITopicMiddleware
{
    readonly IServiceProvider _services;

    public TopicMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(ITopicContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

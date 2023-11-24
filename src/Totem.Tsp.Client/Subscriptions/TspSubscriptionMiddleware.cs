namespace Totem.Subscriptions;

public sealed class TspSubscriptionMiddleware : ITspSubscriptionMiddleware
{
    readonly Func<ITspSubscriptionContext<ITspSubscription>, Func<Task>, CancellationToken, Task> _middleware;

    public TspSubscriptionMiddleware(Func<ITspSubscriptionContext<ITspSubscription>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(ITspSubscriptionContext<ITspSubscription> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class TspSubscriptionMiddleware<TService> : ITspSubscriptionMiddleware
    where TService : ITspSubscriptionMiddleware
{
    readonly IServiceProvider _services;

    public TspSubscriptionMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(ITspSubscriptionContext<ITspSubscription> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

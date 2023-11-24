namespace Totem.Subscriptions;

public sealed class SubscriptionMiddleware : ISubscriptionMiddleware
{
    readonly Func<ISubscriptionContext<ISubscription>, Func<Task>, CancellationToken, Task> _middleware;

    public SubscriptionMiddleware(Func<ISubscriptionContext<ISubscription>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(ISubscriptionContext<ISubscription> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class SubscriptionMiddleware<TService> : ISubscriptionMiddleware
    where TService : ISubscriptionMiddleware
{
    readonly IServiceProvider _services;

    public SubscriptionMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(ISubscriptionContext<ISubscription> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}

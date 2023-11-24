namespace Totem.Subscriptions;

public sealed class SubscriptionHandlerMiddleware : ISubscriptionMiddleware
{
    delegate Task CompiledHandler(ISubscriptionContext<ISubscription> context, CancellationToken cancellationToken);

    readonly ConcurrentDictionary<SubscriptionType, CompiledHandler> _handlersBySubscription = new();
    readonly IServiceProvider _services;

    public SubscriptionHandlerMiddleware(IServiceProvider services) =>
        _services = services;

    public async Task InvokeAsync(ISubscriptionContext<ISubscription> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var handler = _handlersBySubscription.GetOrAdd(context.SubscriptionType, CompileHandler);

        await handler(context, cancellationToken);

        context.ExpectNoErrors();

        await next();
    }

    CompiledHandler CompileHandler(SubscriptionType subscription)
    {
        // (context, cancellationToken) => HandleAsync<TSubscription>(context, cancellationToken)

        var contextParameter = Expression.Parameter(typeof(IEventContext<IEvent>), "context");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
        var call = Expression.Call(
            Expression.Constant(this),
            nameof(HandleAsync),
            new[] { subscription.DeclaredType },
            contextParameter,
            cancellationTokenParameter);

        return Expression.Lambda<CompiledHandler>(call, contextParameter, cancellationTokenParameter).Compile();
    }

    async Task HandleAsync<TSubscription>(ISubscriptionContext<ISubscription> context, CancellationToken cancellationToken)
        where TSubscription : ISubscription
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ISubscriptionHandler<TSubscription>>();

        await handler.HandleAsync((ISubscriptionContext<TSubscription>) context, cancellationToken);
    }
}

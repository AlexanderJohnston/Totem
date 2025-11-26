namespace Totem.Subscriptions;

public interface ITspSubscriptionMiddleware
{
    Task InvokeAsync(ITspSubscriptionContext<ITspSubscription> context, Func<Task> next, CancellationToken cancellationToken);
}

namespace Totem.Subscriptions;

public interface ISubscriptionMiddleware
{
    Task InvokeAsync(ISubscriptionContext<ISubscription> context, Func<Task> next, CancellationToken cancellationToken);
}

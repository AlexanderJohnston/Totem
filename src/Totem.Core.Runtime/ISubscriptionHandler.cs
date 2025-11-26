namespace Totem;

public interface ISubscriptionHandler<in TSubscription> where TSubscription : ISubscription
{
    Task HandleAsync(ISubscriptionContext<TSubscription> context, CancellationToken cancellationToken);
}

namespace Totem.Subscriptions;

public interface ISubscriptionPipeline
{
    Task<ISubscriptionContext<ISubscription>> RunAsync(SubscriptionEnvelope envelope, CancellationToken cancellationToken);
}

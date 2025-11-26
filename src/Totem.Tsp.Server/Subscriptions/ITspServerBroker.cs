namespace Totem.Subscriptions;

public interface ITspServerBroker
{
    Task<ISubscriptionReference> SubscribeAsync(SubscriptionEnvelope subscription, CancellationToken cancellationToken);
}

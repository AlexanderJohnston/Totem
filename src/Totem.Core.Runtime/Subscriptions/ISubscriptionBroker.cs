namespace Totem.Subscriptions;

public interface ISubscriptionBroker
{
    Task SubscribeAsync(SubscriptionEnvelope subscription, CancellationToken cancellationToken);
    Task UnsubscribeAsync(SubscriptionAddress address, CancellationToken cancellationToken);
    Task UnsubscribeAllAsync(Id subscriberId, CancellationToken cancellationToken);
}

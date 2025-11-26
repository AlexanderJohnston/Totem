namespace Totem.Subscriptions;

internal sealed class Subscriber
{
    readonly Dictionary<Id, ISubscriptionReference> _subscriptionsById = new();
    readonly SubscriptionBroker _broker;
    readonly Id _subscriberId;

    internal Subscriber(SubscriptionBroker broker, Id subscriberId)
    {
        _broker = broker;
        _subscriberId = subscriberId;
    }

    internal Subscriber Subscribe(Id subscriptionId, ISubscriptionReference subscription)
    {
        lock(_subscriptionsById)
        {
            if(_subscriptionsById.ContainsKey(subscriptionId))
                throw new ArgumentException($"Expected subscription id to not exist: {subscriptionId}", nameof(subscriptionId));

            _subscriptionsById[subscriptionId] = subscription;
        }

        return this;
    }

    internal async Task UnsubscribeAsync(Id subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = null as ISubscriptionReference;

        lock(_subscriptionsById)
        {
            _subscriptionsById.Remove(subscriptionId, out subscription);

            if(_subscriptionsById.Count == 0)
            {
                _broker.RemoveSubscriber(_subscriberId);
            }
        }

        if(subscription is not null)
        {
            await subscription.UnsubscribeAsync(cancellationToken);
        }
    }

    internal async Task UnsubscribeAllAsync(CancellationToken cancellationToken)
    {
        var subscriptions = null as ISubscriptionReference[];

        lock(_subscriptionsById)
        {
            subscriptions = _subscriptionsById.Values.ToArray();

            _broker.RemoveSubscriber(_subscriberId);
        }

        await Task.WhenAll(
            from subscription in subscriptions
            select subscription.UnsubscribeAsync(cancellationToken));
    }
}

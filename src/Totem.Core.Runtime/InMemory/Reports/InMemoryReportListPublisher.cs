namespace Totem.InMemory.Reports;

internal sealed class InMemoryReportListPublisher
{
    readonly Dictionary<SubscriptionAddress, InMemoryReportListSubscription> _subscriptionsByAddress = new();
    readonly InMemoryReportBroker _broker;

    internal InMemoryReportListPublisher(InMemoryReportBroker broker) =>
        _broker = broker;

    internal ISubscriptionReference Subscribe(SubscriptionAddress address, TimelineVersion checkpoint)
    {
        lock(_subscriptionsByAddress)
        {
            if(!_subscriptionsByAddress.TryGetValue(address, out var subscription))
            {
                subscription = new(this, address);

                _subscriptionsByAddress[address] = subscription;
            }

            subscription.Subscribe(checkpoint);

            return subscription;
        }
    }

    internal void PublishChanged(TimelineVersion newVersion)
    {
        lock(_subscriptionsByAddress)
        {
            foreach(var subscription in _subscriptionsByAddress.Values)
            {
                subscription.PublishChanged(newVersion);
            }
        }
    }

    internal void PublishChanged(SubscriptionAddress address) =>
        _broker.PublishChanged(address);

    internal void RemoveSubscription(SubscriptionAddress address)
    {
        lock(_subscriptionsByAddress)
        {
            _subscriptionsByAddress.Remove(address);
        }
    }
}

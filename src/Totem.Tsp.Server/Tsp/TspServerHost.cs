namespace Totem.Tsp;

public sealed class TspServerHost : ITspServerBroker, ITspWatcher
{
    readonly ConcurrentDictionary<SubscriptionAddress, bool> _addresses = new();
    readonly ISubscriptionPipeline _subscriptionPipeline;
    readonly ITspNotificationPipeline _notificationPipeline;

    public TspServerHost(ISubscriptionPipeline subscriptionPipeline, ITspNotificationPipeline notificationPipeline)
    {
        _subscriptionPipeline = subscriptionPipeline;
        _notificationPipeline = notificationPipeline;
    }

    public async Task<ISubscriptionReference> SubscribeAsync(SubscriptionEnvelope subscription, CancellationToken cancellationToken)
    {
        var address = subscription.Address;

        if(_addresses.ContainsKey(address))
            throw new ArgumentException($"Expected available subscription address: {address}", nameof(subscription));

        var context = await _subscriptionPipeline.RunAsync(subscription, cancellationToken);

        context.ExpectNoErrors();

        if(context.Reference is null)
            throw new Exception($"Expected pipeline {_subscriptionPipeline} to set a reference to the subscription");

        _addresses[address] = true;

        return new TspServerSubscriptionReference(this, address, context.Reference);
    }

    public async Task NotifyAsync(TspNotificationEnvelope notification, CancellationToken cancellationToken)
    {
        if(_addresses.ContainsKey(notification.Address))
        {
            var context = await _notificationPipeline.RunAsync(notification, cancellationToken);

            context.ExpectNoErrors();
        }
    }

    internal void RemoveSubscription(SubscriptionAddress address) =>
        _addresses.Remove(address, out _);
}

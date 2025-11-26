namespace Totem.Subscriptions;

public sealed class SubscriptionBroker : ISubscriptionBroker
{
    readonly ConcurrentDictionary<Id, Subscriber> _subscribersById = new();
    readonly ISubscriptionPipeline _pipeline;

    public SubscriptionBroker(ISubscriptionPipeline pipeline) =>
        _pipeline = pipeline;

    public async Task SubscribeAsync(SubscriptionEnvelope subscription, CancellationToken cancellationToken)
    {
        var context = await _pipeline.RunAsync(subscription, cancellationToken);

        context.ExpectNoErrors();

        var reference = context.Reference;

        if(reference is null)
            throw new Exception($"Expected pipeline {_pipeline} to set a reference to the subscription");

        var subscriberId = subscription.Address.SubscriberId;
        var subscriptionId = subscription.Address.SubscriptionId;

        _subscribersById.AddOrUpdate(
            subscriberId,
            _ => new Subscriber(this, subscriberId).Subscribe(subscriptionId, reference),
            (_, subscriber) => subscriber.Subscribe(subscriptionId, reference));
    }

    public async Task UnsubscribeAsync(SubscriptionAddress address, CancellationToken cancellationToken)
    {
        if(_subscribersById.TryGetValue(address.SubscriberId, out var subscriber))
        {
            await subscriber.UnsubscribeAsync(address.SubscriptionId, cancellationToken);
        }
    }

    public async Task UnsubscribeAllAsync(Id subscriberId, CancellationToken cancellationToken)
    {
        if(_subscribersById.TryGetValue(subscriberId, out var subscriber))
        {
            await subscriber.UnsubscribeAllAsync(cancellationToken);
        }
    }

    internal void RemoveSubscriber(Id subscriberId) =>
        _subscribersById.TryRemove(subscriberId, out _);
}

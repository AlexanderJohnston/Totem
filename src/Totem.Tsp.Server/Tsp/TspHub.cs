namespace Totem.Tsp;

public sealed class TspHub : Hub
{
    readonly ITspServerSerializer _serializer;
    readonly ISubscriptionBroker _broker;

    public TspHub(ITspServerSerializer serializer, ISubscriptionBroker broker)
    {
        _serializer = serializer;
        _broker = broker;
    }

    public async Task<string> Subscribe(string subscriptionType, string data)
    {
        var subscriptionId = Id.NewId();
        var address = GetAddress(subscriptionId);
        var subscription = _serializer.DeserializeSubscription(subscriptionType, data);
        var envelope = new SubscriptionEnvelope(subscription, address);

        await _broker.SubscribeAsync(envelope, CancellationToken.None);

        return subscriptionId.ToString();
    }

    public Task Unsubscribe(string subscriptionId) =>
        _broker.UnsubscribeAsync(GetAddress((Id) subscriptionId), CancellationToken.None);

    public Task UnsubscribeAll() =>
        _broker.UnsubscribeAllAsync(SubscriberId, CancellationToken.None);

    public override Task OnDisconnectedAsync(Exception? exception) =>
        UnsubscribeAll();

    Id SubscriberId => (Id) Context.ConnectionId;

    SubscriptionAddress GetAddress(Id subscriptionId) =>
        new(SubscriberId, subscriptionId);
}

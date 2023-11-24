namespace Totem.Tsp;

public sealed class TotemTspClient : ITotemTspClient
{
    readonly ITspSubscriptionPipeline _subscriptionPipeline;

    public TotemTspClient(ITspSubscriptionPipeline subscriptionPipeline) =>
        _subscriptionPipeline = subscriptionPipeline;

    public Task<ITspSubscriptionContext<ITspSubscription>> SendAsync(TspSubscriptionEnvelope subscription, CancellationToken cancellationToken) =>
        _subscriptionPipeline.RunAsync(subscription, cancellationToken);
}

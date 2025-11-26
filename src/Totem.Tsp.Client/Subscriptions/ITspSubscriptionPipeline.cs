namespace Totem.Subscriptions;

public interface ITspSubscriptionPipeline
{
    Task<ITspSubscriptionContext<ITspSubscription>> RunAsync(TspSubscriptionEnvelope envelope, CancellationToken cancellationToken);
}

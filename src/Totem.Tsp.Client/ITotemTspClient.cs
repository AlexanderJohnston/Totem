namespace Totem;

public interface ITotemTspClient
{
    Task<ITspSubscriptionContext<ITspSubscription>> SendAsync(TspSubscriptionEnvelope subscription, CancellationToken cancellationToken);
}

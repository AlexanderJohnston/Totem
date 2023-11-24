namespace Totem.Subscriptions;

public interface ISubscriptionReference
{
    Task UnsubscribeAsync(CancellationToken cancellationToken);
}

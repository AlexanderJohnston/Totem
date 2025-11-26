namespace Totem.Reports.Subscriptions;

public interface IReportBroker
{
    Task<ISubscriptionReference> SubscribeAsync(ISubscriptionContext<WatchReport> context, CancellationToken cancellationToken);
}

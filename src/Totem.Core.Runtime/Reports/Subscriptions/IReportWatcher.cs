namespace Totem.Reports.Subscriptions;

public interface IReportWatcher
{
    Task NotifyChangedAsync(SubscriptionAddress address, EnvelopeInfo envelopeInfo, CancellationToken cancellationToken);
}

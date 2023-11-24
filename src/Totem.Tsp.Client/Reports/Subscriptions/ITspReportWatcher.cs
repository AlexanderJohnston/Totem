namespace Totem.Reports.Subscriptions;

public interface ITspReportWatcher
{
    Task NotifyChangedAsync(SubscriptionAddress address, EnvelopeInfo envelopeInfo, CancellationToken cancellationToken);
}

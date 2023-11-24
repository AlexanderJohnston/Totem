namespace Totem.Reports.Subscriptions;

public sealed class TspReportWatcher : IReportWatcher
{
    readonly ITspWatcher _tspWatcher;

    public TspReportWatcher(ITspWatcher tspWatcher) =>
        _tspWatcher = tspWatcher;

    public async Task NotifyChangedAsync(SubscriptionAddress address, EnvelopeInfo envelopeInfo, CancellationToken cancellationToken)
    {
        var changed = TspServerEnvelope.Notification(new TspReportChanged(), address, envelopeInfo);

        await _tspWatcher.NotifyAsync(changed, cancellationToken);
    }
}

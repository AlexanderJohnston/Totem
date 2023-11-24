namespace Totem.Reports.Subscriptions;

public sealed class TspReportChangedHandler : INotificationHandler<TspReportChanged>
{
    readonly IEnumerable<ITspReportWatcher> _watchers;

    public TspReportChangedHandler(IEnumerable<ITspReportWatcher> watchers) =>
        _watchers = watchers;

    public Task HandleAsync(INotificationContext<TspReportChanged> context, CancellationToken cancellationToken) =>
        Task.WhenAll(
            from watcher in _watchers
            select watcher.NotifyChangedAsync(context.Address, context.EnvelopeInfo, cancellationToken));
}

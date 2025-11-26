namespace Totem.Reports.Subscriptions;

public sealed class ReportChangedHandler : INotificationHandler<ReportChanged>
{
    readonly IEnumerable<IReportWatcher> _watchers;

    public ReportChangedHandler(IEnumerable<IReportWatcher> watchers) =>
        _watchers = watchers;

    public Task HandleAsync(INotificationContext<ReportChanged> context, CancellationToken cancellationToken) =>
        Task.WhenAll(
            from watcher in _watchers
            select watcher.NotifyChangedAsync(context.Address, context.EnvelopeInfo, cancellationToken));
}

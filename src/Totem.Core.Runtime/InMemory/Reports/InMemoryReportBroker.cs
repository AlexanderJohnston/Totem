namespace Totem.InMemory.Reports;

public sealed class InMemoryReportBroker : IReportBroker, IInMemoryReportBroker
{
    readonly ConcurrentDictionary<(ReportType, Id), InMemoryReportPublisher> _publishersByReportKey = new();
    readonly ConcurrentDictionary<ReportType, InMemoryReportListPublisher> _listPublishersByReport = new();
    readonly RuntimeMap _map;
    readonly IInMemoryNotificationBus _notificationBus;

    public InMemoryReportBroker(RuntimeMap map, IInMemoryNotificationBus notificationBus)
    {
        _map = map;
        _notificationBus = notificationBus;
    }

    public Task<ISubscriptionReference> SubscribeAsync(ISubscriptionContext<WatchReport> context, CancellationToken cancellationToken)
    {
        var address = context.Address;
        var rowType = context.Subscription.ETag.RowInfo.DeclaredType;
        var scope = context.Subscription.ETag.Scope;
        var checkpoint = context.Subscription.ETag.Checkpoint;

        if(!_map.ReportRows.TryGet(rowType, out var row))
            throw RuntimeErrors.ReportRowTypeNotFound.ToException($"Expected mapped row of type {rowType}");

        var reference = scope == ReportQueryScope.Row
            ? GetOrAddPublisher(row.Report, checkpoint.TimelineId).Subscribe(address, checkpoint.Position)
            : GetOrAddListPublisher(row.Report).Subscribe(address, checkpoint);

        return Task.FromResult(reference);
    }

    public void PublishChanged(ReportType report, TimelineVersion newVersion)
    {
        GetOrAddPublisher(report, newVersion.TimelineId).PublishChanged(newVersion.Position);
        GetOrAddListPublisher(report).PublishChanged(newVersion);
    }

    internal void PublishChanged(SubscriptionAddress address) =>
        _notificationBus.Publish(new NotificationEnvelope(new ReportChanged(), address));

    internal void RemovePublisher(ReportType report, Id id) =>
        _publishersByReportKey.TryRemove((report, id), out var _);

    internal void RemoveListPublisher(ReportType report) =>
        _listPublishersByReport.TryRemove(report, out var _);

    InMemoryReportPublisher GetOrAddPublisher(ReportType report, Id id) =>
        _publishersByReportKey.GetOrAdd((report, id), _ => new(this));

    InMemoryReportListPublisher GetOrAddListPublisher(ReportType report) =>
        _listPublishersByReport.GetOrAdd(report, _ => new(this));
}

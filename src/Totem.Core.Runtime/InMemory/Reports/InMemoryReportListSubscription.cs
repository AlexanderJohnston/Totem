namespace Totem.InMemory.Reports;

internal sealed class InMemoryReportListSubscription : ISubscriptionReference
{
    readonly InMemoryReportListPublisher _publisher;
    readonly SubscriptionAddress _address;
    TimelineVersion _version;

    internal InMemoryReportListSubscription(InMemoryReportListPublisher publisher, SubscriptionAddress address)
    {
        _publisher = publisher;
        _address = address;
        _version = TimelineVersion.EmptyList;
    }

    internal void Subscribe(TimelineVersion checkpoint)
    {
        if(checkpoint != _version)
        {
            _publisher.PublishChanged(_address);
        }
    }

    internal void PublishChanged(TimelineVersion newVersion)
    {
        _version = newVersion;

        _publisher.PublishChanged(_address);
    }

    public Task UnsubscribeAsync(CancellationToken cancellationToken)
    {
        _publisher.RemoveSubscription(_address);

        return Task.CompletedTask;
    }
}

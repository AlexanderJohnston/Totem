namespace Totem.InMemory.Reports;

internal sealed class InMemoryReportSubscription : ISubscriptionReference
{
    readonly InMemoryReportPublisher _publisher;
    readonly SubscriptionAddress _address;
    TimelinePosition _position;

    internal InMemoryReportSubscription(InMemoryReportPublisher publisher, SubscriptionAddress address)
    {
        _publisher = publisher;
        _address = address;
    }

    internal void Subscribe(TimelinePosition checkpoint)
    {
        if(checkpoint != _position)
        {
            _publisher.PublishChanged(_address);
        }
    }

    internal void PublishChanged(TimelinePosition newPosition)
    {
        _position = newPosition;

        _publisher.PublishChanged(_address);
    }

    public Task UnsubscribeAsync(CancellationToken cancellationToken)
    {
        _publisher.RemoveSubscription(_address);

        return Task.CompletedTask;
    }
}

namespace Totem.Reports.Subscriptions;

public sealed class WatchReportHandler : ISubscriptionHandler<WatchReport>
{
    readonly IReportBroker _broker;

    public WatchReportHandler(IReportBroker broker) =>
        _broker = broker;

    public async Task HandleAsync(ISubscriptionContext<WatchReport> context, CancellationToken cancellationToken) =>
        context.Reference = await _broker.SubscribeAsync(context, cancellationToken);
}

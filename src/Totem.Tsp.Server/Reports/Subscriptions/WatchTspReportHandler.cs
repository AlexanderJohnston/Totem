namespace Totem.Reports.Subscriptions;

public sealed class WatchTspReportHandler : ISubscriptionHandler<WatchTspReport>
{
    readonly IReportQueryETagFormat _etagFormat;
    readonly ITspServerBroker _broker;

    public WatchTspReportHandler(IReportQueryETagFormat etagFormat, ITspServerBroker broker)
    {
        _etagFormat = etagFormat;
        _broker = broker;
    }

    public async Task HandleAsync(ISubscriptionContext<WatchTspReport> context, CancellationToken cancellationToken)
    {
        if(!_etagFormat.TryDecode(context.Subscription.ETag, out var etag))
        {
            context.AddError(TspErrors.DecodeReportQueryETagFailed);
            return;
        }

        var watch = new SubscriptionEnvelope(new WatchReport(etag), context.Address, context.CorrelationId, context.Principal);

        context.Reference = await _broker.SubscribeAsync(watch, cancellationToken);
    }
}

namespace Totem.Subscriptions;

public sealed class SubscriptionPipeline : ISubscriptionPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<ISubscriptionMiddleware> _steps;
    readonly RuntimeMap _map;

    public SubscriptionPipeline(ILogger<SubscriptionPipeline> logger, IReadOnlyList<ISubscriptionMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<ISubscriptionContext<ISubscription>> RunAsync(SubscriptionEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = _map.CreateContext(envelope);
        var subscriptionType = context.SubscriptionType;
        var subscriptionId = context.Address.SubscriptionId;

        _logger.LogDebug("Run subscription pipeline for {SubscriptionType:l}.{SubscriptionId:l}", subscriptionType, subscriptionId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Subscription pipeline cancelled for {SubscriptionType:l}.{SubscriptionId:l}", subscriptionType, subscriptionId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Subscription pipeline complete for {SubscriptionType:l}.{SubscriptionId:l}", subscriptionType, subscriptionId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}

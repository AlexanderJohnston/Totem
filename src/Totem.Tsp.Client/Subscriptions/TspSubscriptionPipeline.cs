namespace Totem.Subscriptions;

public sealed class TspSubscriptionPipeline : ITspSubscriptionPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<ITspSubscriptionMiddleware> _steps;
    readonly TspSubscriptionContextFactory _contextFactory;

    public TspSubscriptionPipeline(ILogger<TspSubscriptionPipeline> logger, IReadOnlyList<ITspSubscriptionMiddleware> steps)
    {
        _logger = logger;
        _steps = steps;
        _contextFactory = new();
    }

    public async Task<ITspSubscriptionContext<ITspSubscription>> RunAsync(TspSubscriptionEnvelope envelope, CancellationToken cancellationToken)
    {
        var subscriptionType = envelope.SubscriptionType;
        var subscriptionId = envelope.SubscriptionId;

        _logger.LogDebug("Run command pipeline for {SubscriptionType:l}.{SubscriptionId:l}", subscriptionType, subscriptionId);

        var context = _contextFactory.Create(envelope);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Command pipeline cancelled for {SubscriptionType:l}.{SubscriptionId:l}", subscriptionType, subscriptionId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Command pipeline complete for {SubscriptionType:l}.{SubscriptionId:l}", subscriptionType, subscriptionId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}

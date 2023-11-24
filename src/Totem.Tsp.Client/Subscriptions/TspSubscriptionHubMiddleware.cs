namespace Totem.Subscriptions;

public sealed class TspSubscriptionHubMiddleware : ITspSubscriptionMiddleware
{
    readonly ITspHubConnection _hubClient;

    public TspSubscriptionHubMiddleware(ITspHubConnection hubClient) =>
        _hubClient = hubClient;

    public async Task InvokeAsync(ITspSubscriptionContext<ITspSubscription> context, Func<Task> next, CancellationToken cancellationToken)
    {
        await _hubClient.SubscribeAsync(context.Envelope, cancellationToken);

        await next();
    }
}

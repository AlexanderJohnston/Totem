namespace Totem.Notifications;

public sealed class TspNotificationHubMiddleware : ITspNotificationMiddleware
{
    readonly IHubContext<TspHub> _hubContext;
    readonly ITspServerSerializer _serializer;

    public TspNotificationHubMiddleware(IHubContext<TspHub> hubContext, ITspServerSerializer serializer)
    {
        _hubContext = hubContext;
        _serializer = serializer;
    }

    public async Task InvokeAsync(ITspNotificationContext<ITspNotification> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var subscriberId = context.Address.SubscriberId.ToString();
        var subscriptionId = context.Address.SubscriptionId;
        var externalType = context.NotificationInfo.ExternalType;
        var data = _serializer.SerializeNotification(context.Notification);
        var subscriber = _hubContext.Clients.Client(subscriberId);

        await subscriber.SendAsync("handleNotification", subscriptionId, externalType, data, cancellationToken);
    }
}

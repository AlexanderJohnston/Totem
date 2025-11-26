namespace Totem.Tsp;

public sealed class TspHubConnection : ITspHubConnection
{
    readonly CancellationTokenSource _cancellation = new();
    readonly HubConnection _connection;
    readonly IDisposable _handler;
    readonly ITspSubscriptionPipeline _subscriptionPipeline;

    public TspHubConnection(
        HubConnection connection,
        ITspClientSerializer serializer,
        ITspSubscriptionPipeline subscriptionPipeline,
        INotificationPipeline notificationPipeline)
    {
        _connection = connection;
        _subscriptionPipeline = subscriptionPipeline;

        _handler = connection.On("handleNotification", async (string subscriptionId, string notificationType, string data) =>
        {
            if(connection.ConnectionId is null)
                throw new Exception("Expected connection id to have a value");

            var address = new SubscriptionAddress((Id) connection.ConnectionId, (Id) subscriptionId);
            var notification = serializer.DeserializeNotification(notificationType, data);
            var envelope = new NotificationEnvelope(notification, address);
            var context = await notificationPipeline.RunAsync(envelope, _cancellation.Token);

            context.ExpectNoErrors();
        });
    }

    public Task ConnectAsync(CancellationToken cancellationToken) =>
        _connection.StartAsync(cancellationToken);

    public async Task SubscribeAsync(TspSubscriptionEnvelope subscription, CancellationToken cancellationToken)
    {
        var context = await _subscriptionPipeline.RunAsync(subscription, cancellationToken);

        context.ExpectNoErrors();
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        _handler.Dispose();

        await _connection.DisposeAsync();
    }
}

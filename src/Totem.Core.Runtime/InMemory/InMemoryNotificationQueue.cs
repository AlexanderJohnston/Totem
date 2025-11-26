namespace Totem.InMemory;

internal sealed class InMemoryNotificationQueue : IDisposable
{
    readonly Channel<NotificationEnvelope> _channel = Channel.CreateUnbounded<NotificationEnvelope>(new UnboundedChannelOptions
    {
        SingleReader = true
    });
    readonly CancellationTokenSource _cancellation = new();
    readonly ILogger _logger;
    readonly Type _notificationType;
    readonly INotificationPipeline _pipeline;

    internal InMemoryNotificationQueue(ILogger<InMemoryNotificationQueue> logger, Type notificationType, INotificationPipeline pipeline)
    {
        _logger = logger;
        _notificationType = notificationType;
        _pipeline = pipeline;

        Task.Run(ObserveAsync);
    }

    internal void Enqueue(NotificationEnvelope notification)
    {
        _logger.LogTrace("Enqueue notification {NotificationType:l}.{NotificationId:l}", _notificationType, notification.NotificationId);

        _channel.Writer.TryWrite(notification);
    }

    public void Dispose()
    {
        _channel.Writer.Complete();
        _cancellation.Cancel();
    }

    async Task ObserveAsync()
    {
        try
        {
            while(await _channel.Reader.WaitToReadAsync(_cancellation.Token))
            {
                if(_channel.Reader.TryRead(out var notification))
                {
                    await RunPipelineAsync(notification);
                }
            }
        }
        catch(OperationCanceledException)
        { }
        catch(ChannelClosedException)
        { }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Notification queue for {NotificationType:l} failed and has closed", _notificationType);
        }
    }

    async Task RunPipelineAsync(NotificationEnvelope notification)
    {
        var context = await _pipeline.RunAsync(notification, _cancellation.Token);

        context.ExpectNoErrors();
    }
}

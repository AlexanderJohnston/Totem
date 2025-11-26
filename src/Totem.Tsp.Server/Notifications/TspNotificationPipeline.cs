namespace Totem.Notifications;

public sealed class TspNotificationPipeline : ITspNotificationPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<ITspNotificationMiddleware> _steps;
    readonly TspNotificationContextFactory _contextFactory;

    public TspNotificationPipeline(ILogger<TspNotificationPipeline> logger, IReadOnlyList<ITspNotificationMiddleware> steps)
    {
        _logger = logger;
        _steps = steps;
        _contextFactory = new();
    }

    public async Task<ITspNotificationContext<ITspNotification>> RunAsync(TspNotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var notificationType = envelope.NotificationType;
        var notificationId = envelope.NotificationId;

        _logger.LogDebug("Run command pipeline for {NotificationType:l}.{NotificationId:l}", notificationType, notificationId);

        var context = _contextFactory.Create(envelope);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("TSP notification pipeline cancelled for {NotificationType:l}.{NotificationId:l}", notificationType, notificationId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("TSP notification pipeline complete for {NotificationType:l}.{NotificationId:l}", notificationType, notificationId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}

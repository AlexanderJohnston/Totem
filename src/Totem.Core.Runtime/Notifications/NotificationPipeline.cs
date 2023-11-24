namespace Totem.Notifications;

public sealed class NotificationPipeline : INotificationPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<INotificationMiddleware> _steps;
    readonly RuntimeMap _map;

    public NotificationPipeline(ILogger<NotificationPipeline> logger, IReadOnlyList<INotificationMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<INotificationContext<INotification>> RunAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = _map.CreateContext(envelope);
        var notificationType = context.NotificationType;
        var notificationId = context.NotificationId;

        _logger.LogDebug("Run pipeline for {NotificationType:l}.{NotificationId:l}", notificationType, notificationId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Notification pipeline cancelled for {NotificationType:l}.{NotificationId:l}", notificationType, notificationId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Notification pipeline complete for {NotificationType:l}.{NotificationId:l}", notificationType, notificationId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}

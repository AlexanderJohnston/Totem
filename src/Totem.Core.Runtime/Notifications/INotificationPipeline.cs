namespace Totem.Notifications;

public interface INotificationPipeline
{
    Task<INotificationContext<INotification>> RunAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);
}

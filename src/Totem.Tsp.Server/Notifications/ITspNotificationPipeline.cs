namespace Totem.Notifications;

public interface ITspNotificationPipeline
{
    Task<ITspNotificationContext<ITspNotification>> RunAsync(TspNotificationEnvelope notification, CancellationToken cancellationToken);
}

namespace Totem.Notifications;

public interface ITspWatcher
{
    Task NotifyAsync(TspNotificationEnvelope notification, CancellationToken cancellationToken);
}

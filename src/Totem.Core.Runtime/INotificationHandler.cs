namespace Totem;

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task HandleAsync(INotificationContext<TNotification> context, CancellationToken cancellationToken);
}

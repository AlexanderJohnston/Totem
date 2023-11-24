namespace Totem.Notifications;

public interface INotificationMiddleware
{
    Task InvokeAsync(INotificationContext<INotification> context, Func<Task> next, CancellationToken cancellationToken);
}

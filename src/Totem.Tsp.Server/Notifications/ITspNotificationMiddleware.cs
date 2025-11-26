namespace Totem.Notifications;

public interface ITspNotificationMiddleware
{
    Task InvokeAsync(ITspNotificationContext<ITspNotification> context, Func<Task> next, CancellationToken cancellationToken);
}

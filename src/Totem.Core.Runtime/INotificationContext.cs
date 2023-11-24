namespace Totem;

public interface INotificationContext<out TNotification> : IMessageContext
    where TNotification : INotification
{
    new NotificationEnvelope Envelope { get; }
    TNotification Notification { get; }
    NotificationInfo NotificationInfo { get; }
    NotificationType NotificationType { get; }
    Id NotificationId { get; }
    Id SubscriberId { get; }
    Id SubscriptionId { get; }
    SubscriptionAddress Address { get; }
}

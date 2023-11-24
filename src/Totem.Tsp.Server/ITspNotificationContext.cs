namespace Totem;

public interface ITspNotificationContext<out TNotification> : IMessageContext
    where TNotification : ITspNotification
{
    new TspNotificationEnvelope Envelope { get; }
    TNotification Notification { get; }
    NotificationInfo NotificationInfo { get; }
    NotificationType NotificationType { get; }
    Id NotificationId { get; }
    Id SubscriberId { get; }
    Id SubscriptionId { get; }
    SubscriptionAddress Address { get; }
}

namespace Totem.Notifications;

public sealed class TspNotificationEnvelope : MessageEnvelope
{
    public TspNotificationEnvelope(EnvelopeInfo info, ITspNotification notification, SubscriptionAddress address) : base(info)
    {
        Notification = notification;
        NotificationInfo = NotificationInfo.From(notification.GetType());
        Address = address;
    }

    public ITspNotification Notification { get; }
    public NotificationInfo NotificationInfo { get; }
    public Type NotificationType => NotificationInfo.DeclaredType;
    public Id NotificationId => Info.MessageId;
    public SubscriptionAddress Address { get; }
}

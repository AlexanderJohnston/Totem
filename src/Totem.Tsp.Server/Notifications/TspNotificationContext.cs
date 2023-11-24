namespace Totem.Notifications;

public sealed class TspNotificationContext<TNotification> : MessageContext, ITspNotificationContext<TNotification>
    where TNotification : ITspNotification
{
    internal TspNotificationContext(TspNotificationEnvelope envelope, NotificationType notificationType) : base(envelope)
    {
        Notification = (TNotification) envelope.Notification;
        NotificationType = notificationType;
    }

    public new TspNotificationEnvelope Envelope => (TspNotificationEnvelope) base.Envelope;
    public TNotification Notification { get; }
    public NotificationInfo NotificationInfo => Envelope.NotificationInfo;
    public NotificationType NotificationType { get; }
    public Id NotificationId => Envelope.NotificationId;
    public Id SubscriberId => Envelope.Address.SubscriberId;
    public Id SubscriptionId => Envelope.Address.SubscriptionId;
    public SubscriptionAddress Address => Envelope.Address;
}

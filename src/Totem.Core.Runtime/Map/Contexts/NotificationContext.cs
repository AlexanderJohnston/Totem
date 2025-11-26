namespace Totem.Map.Contexts;

internal sealed class NotificationContext<TNotification> : MessageContext, INotificationContext<TNotification>
    where TNotification : INotification
{
    internal NotificationContext(NotificationEnvelope envelope, NotificationType notificationType) : base(envelope)
    {
        Notification = (TNotification) envelope.Notification;
        NotificationType = notificationType;
    }

    public new NotificationEnvelope Envelope => (NotificationEnvelope) base.Envelope;
    public TNotification Notification { get; }
    public NotificationInfo NotificationInfo => Envelope.NotificationInfo;
    public NotificationType NotificationType { get; }
    public Id NotificationId => Envelope.NotificationId;
    public Id SubscriberId => Envelope.Address.SubscriberId;
    public Id SubscriptionId => Envelope.Address.SubscriptionId;
    public SubscriptionAddress Address => Envelope.Address;
}

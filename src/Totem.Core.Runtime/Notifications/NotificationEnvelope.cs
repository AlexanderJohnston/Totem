namespace Totem.Notifications;

public sealed class NotificationEnvelope : MessageEnvelope
{
    public NotificationEnvelope(INotification notification, SubscriptionAddress address, EnvelopeInfo info) : base(info)
    {
        Notification = notification;
        NotificationInfo = NotificationInfo.From(notification.GetType());
        Address = address;
    }

    public NotificationEnvelope(INotification notification, SubscriptionAddress address, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(notification, address, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public INotification Notification { get; }
    public NotificationInfo NotificationInfo { get; }
    public Type NotificationType => NotificationInfo.DeclaredType;
    public Id NotificationId => Info.MessageId;
    public SubscriptionAddress Address { get; }
}

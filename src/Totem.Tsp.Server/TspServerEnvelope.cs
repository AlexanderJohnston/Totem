namespace Totem;

public static class TspServerEnvelope
{
    public static TspNotificationEnvelope Notification(ITspNotification notification, SubscriptionAddress address, EnvelopeInfo info) =>
        new(info, notification, address);

    public static TspNotificationEnvelope Notification(ITspNotification notification, SubscriptionAddress address, Id? correlationId = null, ClaimsPrincipal? principal = null) =>
        Notification(notification, address, new EnvelopeInfo(Id.NewId(), correlationId, principal));
}

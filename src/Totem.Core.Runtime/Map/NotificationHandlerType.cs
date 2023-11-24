namespace Totem.Map;

public sealed class NotificationHandlerType : MessageHandlerType
{
    internal NotificationHandlerType(Type declaredType, Type serviceType, NotificationType notification) : base(declaredType, serviceType) =>
        Notification = notification;

    public NotificationType Notification { get; }
}

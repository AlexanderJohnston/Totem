namespace Totem.Map;

public sealed class NotificationType : MessageType
{
    internal NotificationType(NotificationInfo info) : base(info)
    { }

    public new NotificationInfo Info => (NotificationInfo) base.Info;
    public NotificationHandlerType Handler { get; internal set; } = null!;
}
